using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using System;
using System.Collections.Generic;
using System.Text;
using EncryptDecrypt;
using System.Collections;
using System.IO;
using System.Linq;
namespace ManufacturerDevice
{
    class Program
    {
        public static MacAddress SelectedDeviceMac;
        //initialize manufacturer device DB for possible commands that can be accepted 
        public static databaseCommands db = new databaseCommands();
        static void Main(string[] args)
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Print the list
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.Write((i + 1) + ". " + device.Name);
                if (device.Description != null)
                    Console.WriteLine(" (" + device.Description + ")");
                else
                    Console.WriteLine(" (No description available)");
            }

            int deviceIndex = 0;
            do
            {
                Console.WriteLine("Enter the interface number (1-" + allDevices.Count + "):");
                string deviceIndexString = Console.ReadLine();
                if (!int.TryParse(deviceIndexString, out deviceIndex) ||
                    deviceIndex < 1 || deviceIndex > allDevices.Count)
                {
                    deviceIndex = 0;
                }
            } while (deviceIndex == 0);

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[deviceIndex - 1];

            // Open the device
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                Console.WriteLine("Listening on " + selectedDevice.Description + "...");
                SelectedDeviceMac = PcapDotNet.Core.Extensions.LivePacketDeviceExtensions.GetMacAddress(allDevices[deviceIndex - 1]);
                // start the capture
                communicator.ReceivePackets(0, PacketHandler);
            }
        }

        /// <summary>
        /// Callback function invoked by Pcap.Net for every incoming packet
        /// Receives all incoming packets on the opened connection. 
        /// Only packets with same destination MAC as HW's MAC of opened connection are considered
        /// Valid received packets are decrypted and analyzed in the reverse order as when sent
        /// </summary>
        /// <param name="packet"></param>
        private static void PacketHandler(Packet packet)
        {
            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);

            if (packet.Ethernet.Destination == SelectedDeviceMac)
            {//packet was indeed ment for this device. Also the spam packets sent to keep connection alive have random MAC for destination apparently!
             //no point in checking if packet received is from ethernet layer since we already decided what to monitor,
             //however a simple if packet.DataLink.Kind == DataLinkKind.Ethernet would be enough
                try
                {
                    //get packet payload and parse to bytearray
                    byte[] payload = packet.Ethernet.IpV4.ToMemoryStream().ToArray();

                    byte[] decryptedBytes = RSA.Decrypt(RSA.privateKey, payload);

                    //get endianness from last bit of payload
                    bool y = BitConverter.ToBoolean(new byte[] { decryptedBytes[decryptedBytes.Length - 1] });

                    //take first 4 bytes (int 32bits) for command ID
                    byte[] commandId = decryptedBytes.Take(4).ToArray();

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(commandId);

                    int cmdId = BitConverter.ToInt32(commandId);

                    //argument section is in the middle, at start 4 bytes for ID and at end 1 byte for endianness
                    BitArray argumentsOfCommandPacket = new BitArray(decryptedBytes.Skip(4).Take(decryptedBytes.Length - 5).ToArray());

                    printReceivedCommandPacketDetails(cmdId, argumentsOfCommandPacket);
                }
                catch(Exception e){
                    Console.WriteLine("Could not decrypt received packet.");
                }
            }
        }
        /// <summary>
        /// Used to output received command packet's info
        /// </summary>
        /// <param name="cmdId"></param>
        /// <param name="arguments"></param>
        private static void printReceivedCommandPacketDetails(int cmdId, BitArray arguments)
        {

            command cmd = db.commands.Where(c => c.Key == cmdId).Select(c => c.Value).FirstOrDefault();

            if(cmd == null)
            {//if no command with such ID found, then the HUB has a command with X id in its db that was 
             //attempted to run on this device which does not have said command
                Console.WriteLine("\nA valid command was received but it is not registered with this device yet.\n");
            }
            else
            {
                List<Object> argumentsAsVar = utilities.BitArrayToListObject(arguments, cmd.argumentLengths, cmd.argumentTypes);

                Console.WriteLine("\nPrinting arguments of the received command packet of ID {0} and name {1}:\n", cmdId, cmd.commandName);
                int c = 0;
                foreach (var obj in argumentsAsVar)
                    Console.WriteLine("Argument n. {0} is of type {1} and has a value of {2}.\n",
                        (c += 1), obj.GetType().ToString(), obj.ToString());
                //no point in printing expected argument Length and actual Length because there already are checks when 
                //parsing from BitArray to list of objects, same for argument types!!
            }
        }




        /// <summary>
        /// This function build an Ethernet with payload packet.
        /// </summary>
        private static Packet BuildEthernetPacket(string sourceMAC, string destinationMAC)
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("01:01:01:01:01:01"),
                    Destination = new MacAddress("02:02:02:02:02:02"),
                    EtherType = EthernetType.IpV4,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(Encoding.ASCII.GetBytes("hello world")),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }
    }

}
