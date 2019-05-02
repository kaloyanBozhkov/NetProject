using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using EncryptDecrypt;
using PrivateINet;

namespace Hub
{
    class ResponseLocalhost
    {
        public bool success { get; set; }
        public string information { get; set; }
        public string commandType { get; set; }
    }

    /// <summary>
    /// Same as command object but with List<bool> isntead of BitArray for arguments and int[] instead of byte[] for argumentTypes
    /// Because JSON does not distinguish between those..
    /// </summary>
    class commandFormattedForJSON
    {
        public string commandName { get; set; }
        public int[] argumentLengths { get; set; }//counter for number of bits making up each argument
        private List<bool> arguments { get; set; }
        public int[] argumentTypes { get; set; }
        public void setArgumentTypes(byte[] argTypes)
        {
            argumentTypes = Array.ConvertAll(argTypes, c => (int)c);
        }
        public void setArguments(BitArray args)
        {
            List<bool> argumentsAsList = new List<bool>();
            foreach (bool x in args)
                argumentsAsList.Add(x);

            arguments = argumentsAsList;
        }
        public string description { get; set; }
    }

    class NetInfo
    {
        public int hostport;
        public int netport;
        public string localhost;
    }

    class Program
    {
        //initialize databse
        public static databaseCommands db = new databaseCommands();

        public static NetInfo netinfo = new NetInfo()
        {
            hostport = 4342,
            netport = 4343,
            localhost = "127.0.0.1"
        };

        static void Main(string[] args)
        {
            //LOCALHOST LISTEN FOR COMMANDS
            if (args.Length > 0) netinfo.hostport = Convert.ToInt32(args[0]);
            if (args.Length > 1) netinfo.netport = Convert.ToInt32(args[1]);
            UdpClient listener = new UdpClient(netinfo.netport);
            //Create IP end point to which to send command packets
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(netinfo.localhost), netinfo.hostport);
            try
            {
                while (true)
                {
                    /*HANDLER FOR JOSH*/
                    Console.WriteLine("Waiting for a message from Josh");
                    byte[] bytes = listener.Receive(ref endPoint);
                    Console.WriteLine("Received bytes from {0}\n", endPoint.ToString());
                    string jsonMasterCommand = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                    Console.WriteLine("Received Master Command as JSON string:\n{0}\n", jsonMasterCommand);
                    executeMasterCommand(Jil.JSON.Deserialize<MasterCommand>(jsonMasterCommand));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
            }

            Console.ReadLine();
        }

        static void sendResponse(MasterCommand cmdRespondingTo, bool success, string information = "")
        {
            ResponseLocalhost response = new ResponseLocalhost()
            {
                success = success,
                commandType = cmdRespondingTo.commandType,
                information = information
            };
            string jsonString = Jil.JSON.Serialize(response);
            Socket s = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(netinfo.localhost), netinfo.hostport);
            Console.WriteLine("Sent to 127.0.0.1:" + endPoint.Port + "\n" + jsonString);
            s.SendTo(ASCIIEncoding.ASCII.GetBytes(jsonString), endPoint);
        }

        /// <summary>
        /// This function fires based on MasterCommand received from localhost. The MasterCommand will control this function's actions
        /// </summary>
        static void executeMasterCommand(MasterCommand MS)
        {
            //Example of command packet creation, includes argument check, creation and endianness set. True if bigEndian, flase if littleEndian.
            //CommandPacketSendReceiveExample(true);
            /*
            On "Manufacturer device" run packet listener to detect these commands being sent
            PacketListener();
            */
            switch (MS.commandType)
            {
                case "SEND":
                    MasterCommandSend(MS);
                    break;
                case "MODIFY":
                    MasterCommandModify(MS);
                    break;
                case "REQUEST":
                    MasterCommandRequest(MS);
                    break;
            }
        }

        /// <summary>
        /// Used to retrive a command from the DB to see its details. 
        /// </summary>
        /// <param name="MS"></param>
        static void MasterCommandRequest(MasterCommand MS)
        {
            if (MS.commandId.Length > 0 || MS.commandName.Length > 0)
            {
                int commandId;
                if (MS.commandId == "")
                {
                    commandId = db.commands.Where(c => c.Value.commandName.ToLower() == MS.commandName).Select(c => c.Key).FirstOrDefault();
                }
                else
                {
                    int.TryParse(MS.commandId, out commandId);
                }
                if (db.commands.Where(c => c.Key == commandId).Count() > 0)// COMMAND EXISTS IN DB!!!
                {
                    command cmd = db.commands[(int)commandId];

                    Console.WriteLine("\n\nPRINTING ARGUMENTS OF COMMAND ABOUT TO BE RETURNED TO JOSH THROUGH LOCALHOST\n");
                    utilities.PrintCommandArguments(cmd);

                    if (!MS.deleteRequestedCommand)//NOT A DELETE REQUEST
                    {
                        commandFormattedForJSON cmdToJSONFormat = new commandFormattedForJSON()
                        {
                            argumentLengths = cmd.argumentLengths,
                            argumentTypes = cmd.argumentTypes.Select(x => (int)x).ToArray(),
                            commandName = cmd.commandName,
                            description = cmd.description
                        };

                        cmdToJSONFormat.setArgumentTypes(cmd.argumentTypes);
                        cmdToJSONFormat.setArguments(cmd.arguments);

                        string objToJSON = Jil.JSON.Serialize(cmdToJSONFormat);
                        Console.WriteLine("\n\nJSON string representing command:\n{0}", objToJSON);

                        sendResponse(MS, true, objToJSON);

                        Console.WriteLine("\n\nCommand, serialized as JSON string, was successfully returned to Josh.\n\n");
                    }
                    else
                    {
                        db.commands.Remove(commandId);
                        Console.WriteLine("\n\nCommand of id {0} has been removed from DB successfully.\n", commandId);
                        sendResponse(MS, true, "Command of id " + commandId + " has successfully been removed from DB.");
                    }
                }
                else
                {
                    //failed
                    Console.WriteLine("Could not find command in DB since the id/name are not present.\n\n");
                    sendResponse(MS, false, "Command could not be found in the DB, make sure the command ID or command Name exist first.");
                }
            }
            else
            {
                Console.WriteLine("Could not run REQUEST command since no command ID or Name have been provided.\n\n");
                sendResponse(MS, false, "Request of command could not be achieved since some of the required Master Command object properties are not set: command ID or Name mus tbe set.");
            }

        }

        /// <summary>
        /// Used to modify DB of commands. Either Update or Create.
        /// </summary>
        /// <param name="MS"></param>
        static void MasterCommandModify(MasterCommand MS)
        {
            //check if all values required for operation are set properly
            if (MS.commandId.Length > 0 && MS.argumentLengths.Length > 0 && MS.argumentTypes.Length > 0 && MS.commandName.Length > 0 && MS.description.Length > 0)
            {
                int cmdId;
                int.TryParse(MS.commandId, out cmdId);
                command cmd = new command()
                {
                    argumentLengths = MS.argumentLengths,
                    argumentTypes = MS.getArgumentTypesAsByteArray(),
                    commandName = MS.commandName,
                    arguments = MS.getArgumentsAsBitArray(),
                    description = MS.description
                };
                Console.WriteLine("\n\nPRINTING PASSED ARGUMENTS OF COMMAND PASSED BY JOSH THROUGH LOCALHOST: \n");
                utilities.PrintCommandArguments(cmd);
                if (MS.createNewCommand)
                {
                    if (db.commands.Where(c => c.Key == cmdId).Count() != 0)
                    {
                        //fails, command with that ID already exists!!!
                        Console.WriteLine("\nFailed to insert command to DB. Command of id {0} already exists.\n\n", cmdId);
                        sendResponse(MS, false, "The command with id " + cmdId + " could not be added to DB since a command with same ID already exists. Why don't you try updating it instead fam?.");
                    }
                    else
                    {
                        db.commands.Add(cmdId, cmd);
                        //Success
                        Console.WriteLine("SUCCESSFULLY ADDED CMD WITH {0}\n\n", cmdId);
                        sendResponse(MS, true, "Command of id " + cmdId + " has been added to the DB successfully.");
                    }
                }
                else
                {
                    if (db.commands.Where(c => c.Key == cmdId).Count() == 0)
                    {
                        //fails, command with that ID does not exist, so cannot update
                        Console.WriteLine("Failed to update command of id {0} since no such command exists in DB.", cmdId);
                        sendResponse(MS, false, "The command you are trying to update does not exist. Check that the command with ID " + cmdId + " exists first.");
                    }
                    else
                    {
                        db.commands[cmdId] = cmd;
                        //Success
                        Console.WriteLine("SUCCESSFULLY UPDATED CMD OF ID {0}\n\n", cmdId);
                        sendResponse(MS, true, "Command of id " + cmdId + " has been updated successfully.");
                    }
                }
            }
            else
            {
                //fail instantly
                Console.WriteLine("Could not run MODIFY command since some of the mandatory fields in Master Command are not valid.\n\n");
                sendResponse(MS, false, "Issue adding command, some of the mandatory fields are empty! Check what values you are passing pls.");
            }
        }


        /// <summary>
        /// Sends a command to destination MAC. Either send a command in its default state as its in DB of commands or with custom arguments if they are passed
        /// </summary>
        /// <param name="MS"></param>
        static void MasterCommandSend(MasterCommand MS)
        {
            if ((MS.commandId.Length > 0 || MS.commandName.Length > 0) && MS.destinationMAC.Length > 0)
            {
                int commandId;
                if (MS.commandId == "")
                {
                    commandId = db.commands.Where(c => c.Value.commandName.ToLower() == MS.commandName).Select(c => c.Key).FirstOrDefault();
                }
                else
                {
                    int.TryParse(MS.commandId, out commandId);
                }
                if (db.commands.Where(c => c.Key == commandId).Count() > 0)// COMMAND EXISTS IN DB!!!
                {
                    command cmd = db.commands[(int)commandId];
                    //If custom arguments are passed, set them. REMEMBER TO KEEP SAME ORDER OF BITS AND LENGTH AS argumentLengths and argumentTypes on specific command in DB
                    //this updates db every time we send new arguments, a feature we wanted! This way next time default args are sent it will be the last ones sent
                    if (MS.arguments.Count > 0)
                        cmd.arguments = MS.getArgumentsAsBitArray();

                    Console.WriteLine("\n\nPRINTING ARGUMENTS OF COMMAND ABOUT TO BE SENT\n");

                    utilities.PrintCommandArguments(cmd);

                    Console.WriteLine("Command packet generated and about to be sent.\n COMMAND ID: {0} \n COMMAND NAME: {1}\n", commandId, cmd.commandName);

                    //Send packet to destination mac address via ethernet layer

                    Regex validMAC = new Regex("^([0-9A-F]{2}[:-]){5}([0-9A-F]{2})$");
                    if (validMAC.IsMatch(MS.destinationMAC))
                    {
                        PacketSender(cmd, MS.destinationMAC);
                        Console.WriteLine("Command packet WAS SENT SUCCESSFULLY.\n COMMAND ID: {0} \n COMMAND NAME: {1}\n\n", commandId, cmd.commandName);
                        sendResponse(MS, true, "Command of id " + commandId + " has been sent to destination MAC successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Command of id " + commandId + " could not be sent to destination MAC since the MAC given is incorrect in format\n\n");
                        sendResponse(MS, false, "Command of id " + commandId + " could not be sent to destination MAC since the MAC given is incorrect in format:  " + MS.destinationMAC);
                    }
                }
                else
                {
                    //failed
                    Console.WriteLine("Could not find command in DB since the id/name are not present.\n\n");
                    sendResponse(MS, false, "Command could not be found in the DB, make sure the command ID or command Name exist first.");
                }
            }
            else
            {
                Console.WriteLine("Could not run SEND command since some of the mandatory fields in Master Command are not valid.\n\n");
                sendResponse(MS, false, "Command could not be sent since some of the required Master Command object properties are not set.");
            }

        }

        /// <summary>
        /// Sends a command through selected network device on layer 1
        /// </summary>
        /// <param name="cmd">command object</param>
        /// <param name="destinationMAC">the Manufacturer Device's MAC address</param>
        static void PacketSender(command cmd, string destinationMAC)
        {
            Console.WriteLine("Select which device to send packet through");
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
                Console.WriteLine("Enter the interface number (1-" + allDevices.Count + ") to send through:");
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

                int commandId = db.commands.Where(c => c.Value.commandName.ToLower() == cmd.commandName.ToLower()).Select(c => c.Key).FirstOrDefault();
                Packet packetToSend = BuildEthernetPacket(LivePacketDeviceExtensions.GetMacAddress(allDevices[deviceIndex - 1]).ToString(), destinationMAC, utilities.CreateCommandBytesForSending(commandId, cmd.arguments));
                communicator.SendPacket(packetToSend);
            }
        }

        /// <summary>
        /// Callback function invoked by Pcap.Net for every incoming packet
        /// </summary>
        /// <param name="packet"></param>
        static void PacketHandler(Packet packet)
        {
            //revise add acknowledgement reader, to confirm packet received (have dictionary for this maybe)
            Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
            MacAddress destinationMac = packet.Ethernet.Destination;
            MacAddress sourceMac = packet.Ethernet.Source;
            Datagram payload = packet.Ethernet.Payload;
            byte[] payloadToBytes;
            using (MemoryStream ms = payload.ToMemoryStream())
            {
                payloadToBytes = new byte[payload.Length];
                ms.Read(payloadToBytes, 0, payload.Length);
            }
        }

        static void PacketListener()
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
                Console.WriteLine("Listening on " + selectedDevice.Description + " with MAC address of " + LivePacketDeviceExtensions.GetMacAddress(allDevices[deviceIndex - 1]) + "...");

                // start the capture
                communicator.ReceivePackets(0, PacketHandler);
            }
        }

        /// <summary>
        /// This function builds an ethernet packet with payload packet ready to be sent to destination mac.
        /// </summary>
        static Packet BuildEthernetPacket(string sourceMAC, string destinationMAC, byte[] commandPacket)
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress(sourceMAC), //mac of device were sending from, programmatically fetched
                    Destination = new MacAddress(destinationMAC), //mac of destination manufacturer device's network device
                    EtherType = EthernetType.IpV4,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(commandPacket),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }
    }
}