using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Net;
using System.Linq;

namespace ManufacturerDevice
{
    class commandPacket
    {
        /// <summary>
        /// 32 bits of data for command ID
        /// </summary>
        public int commandID { get; set; }
        /// <summary>
        /// Array of arguments to be passed on send or read on receive.
        /// Count of arguments and Length of each argument found in database based on command ID
        /// </summary>
        public BitArray arguments { get; set; }
    }
    static class utilities
    {
        /// <summary>
        /// Use this to check that the arguments you passed through localhost's Master Command are sent, received and parsed correctly to general Object ready to use!
        /// </summary>
        /// <param name="receivedCommandThroughLocalhost"></param>
        public static void PrintCommandArguments(command receivedCommandThroughLocalhost)
        {
            List<Object> argumentsInBitsToObjects = BitArrayToListObject(receivedCommandThroughLocalhost.arguments, receivedCommandThroughLocalhost.argumentLengths, receivedCommandThroughLocalhost.argumentTypes); ;

            int count = 1;
            foreach (Object o in argumentsInBitsToObjects)
                Console.WriteLine((count++) + ". " + o.ToString() + "\n");
        }

        /// <summary>
        /// Convert an object to a byte array
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ObjectToByteArray(Object obj, int varId = -1)
        {
            varId = varId == -1 ? GetVariableId(obj.GetType().ToString()) : varId;
            switch (varId)
            {
                case 1:
                    return new byte[] { ((bool)obj ? (byte)1 : (byte)0) }; //if obj is true set 1 if false 0
                case 2:
                    return Encoding.ASCII.GetBytes(obj.ToString());
                case 3:
                    return BitConverter.GetBytes((int)obj);
            }
            return new byte[] { };//empty since not of expected var type
        }

        public static void printCommandPacket(commandPacket cmd)
        {
            Console.WriteLine("\n=== PRINTING COMMAND PACKET ===\n");
            Console.WriteLine("ID: " + cmd.commandID + "\nArguments: ");

            List<object> arguments = new List<object>();
            arguments = CommandPacketArgumentBitArrayToListObject(cmd);

            foreach (object arg in arguments)
                Console.WriteLine(arg.GetType() + ": " + arg.ToString());
            Console.WriteLine("\n=== END OF PRINT ===\n");
        }

        /// <summary>
        /// Used to parse bit array data to command object
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static commandPacket BitArrayToCommandPacket(BitArray bitArray)
        {
            //command ID always 32 bits
            int commandIDLength = 32;

            commandPacket cmd = new commandPacket();
            //ID of command to look arguments for in db
            BitArray commandIDToBits = new BitArray(commandIDLength);

            for (int k = 0; k < commandIDLength; k++)
                commandIDToBits[k] = bitArray[k];

            //convert sequence of bits representing command ID to int
            cmd.commandID = BitConverter.ToInt32(BitArrayToByteArray(commandIDToBits), 0);
            cmd.arguments = new BitArray(bitArray.Length - commandIDLength);
            for (int k = 0; k < cmd.arguments.Length; k++)
                cmd.arguments[k] = bitArray[k + 32];

            return cmd;
        }

        /// <summary>
        /// Takes command packet's arguments and parses them to their appropriate types, returning a list of objects containing the command's arguments
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="bitArray"></param>
        /// <returns></returns>
        public static List<object> CommandPacketArgumentBitArrayToListObject(commandPacket cmd)
        {
            int[] argumentLength = Program.db.commands.Where(x => x.Key == cmd.commandID).Select(x => x.Value.argumentLengths).FirstOrDefault().ToArray();
            //helps parsing argument data into correct variable types. 1 is boolean, 2 is string 3 is integer
            byte[] argumentTypes = Program.db.commands.Where(x => x.Key == cmd.commandID).Select(x => x.Value.argumentTypes).FirstOrDefault().ToArray();
            return BitArrayToListObject(cmd.arguments, argumentLength, argumentTypes);
        }

        public static List<object> BitArrayToListObject(BitArray arguments, int[] argumentLength, byte[] argumentTypes)
        {
            List<object> argumentsList = new List<object>();
            //BASED ON LENGTH GET DATA AND PARSE BASED ON TYPE
            int argumentCounter = 0;
            int tmpArgumentCounter = 0;
            int tmpBitCounter = 0;
            BitArray tmpBitArray = new BitArray(argumentTypes[0] == 1 ? 8 : argumentLength[0]);
            for (int k = 0; k < arguments.Length; k++)
            {
                tmpBitArray[tmpBitCounter] = arguments[k];
                tmpBitCounter++;
                tmpArgumentCounter++;
                if ((argumentTypes[argumentCounter] == 1 && tmpArgumentCounter == 8) || (argumentTypes[argumentCounter] != 1 && argumentLength[argumentCounter] == tmpArgumentCounter))
                {//if current argument's bits matches the specific argument's length found inside of the array of argument lengths
                    byte[] sequenceOfBitsToBytes = BitArrayToByteArray(tmpBitArray);
                    switch (argumentTypes[argumentCounter])
                    {
                        case 1:
                            argumentsList.Add(BitConverter.ToBoolean(sequenceOfBitsToBytes, 0));
                            break;
                        case 2:
                            argumentsList.Add(Encoding.ASCII.GetString(sequenceOfBitsToBytes));
                            break;
                        case 3:
                            argumentsList.Add(BitConverter.ToInt32(sequenceOfBitsToBytes, 0));
                            break;
                    }
                    if (argumentLength.Length > (argumentCounter + 1))//if not end of sequence of bits then there are more arguments
                    {
                        argumentCounter++;
                        if (argumentTypes[argumentCounter] == 1)
                            tmpBitArray = new BitArray(8);//expect 8 bits for boolean since its in a byte
                        else
                            tmpBitArray = new BitArray(argumentLength[argumentCounter]); //for everything else expect x number of bits
                        
                        tmpArgumentCounter = 0;
                        tmpBitCounter = 0;
                    }
                }
            }
            return argumentsList;
        }

        /// <summary>
        /// Prints bit array in opposite order to be able to properly see its contents as expected, since C# stores it as little endian. Order of bits is right to left.
        /// </summary>
        /// <param name="bitArray"></param>
        public static void PrintBitArray2(BitArray bitArray)
        {
            for (int k = bitArray.Length - 1; k >= 0; k--)
            {
                Console.Write(bitArray[k] ? "1" : "0");
                Console.Write(k % 8 == 0 ? " " : "");
            }
        }
        /// <summary>
        /// Print contents of bit array. NOTE:  C# stores bit array in little endian order, order of bits is left to right.
        /// </summary>
        /// <param name="bitArray"></param>
        public static void PrintBitArray(BitArray bitArray)
        {
            for (int k = 0; k < bitArray.Length; k++)
            {
                Console.Write(k > 0 && k % 8 == 0 ? " " : "");
                Console.Write(bitArray[k] ? "1" : "0");


            }
        }
        // START UTILS
        /// <summary>
        /// Returns an array of integers, but reversed bit order of each int
        /// </summary>
        /// <param name="arr">Array of integers to transform</param>
        /// <param name="toBigEndian">Is it a big endian that we are converting to?</param>
        /// <returns></returns>
        public static int[] ReverseBitsIntArray(int[] arr, bool toBigEndian)
        {
            List<int> convertedArr = new List<int>();
            foreach (int x in arr)
                convertedArr.Add((toBigEndian ? IPAddress.HostToNetworkOrder(x) : IPAddress.NetworkToHostOrder(x)));

            return convertedArr.ToArray();
        }


        /// <summary>
        /// Reverses byte order of bitarray, MAKE SURE IT IS 8byte or multiple.
        /// </summary>
        /// <param name="bitArray">BitArray to reverse order of</param>
        /// <returns></returns>
        public static BitArray ReverseByteOrder(BitArray bitArray)
        {
            byte[] byteArray = new byte[(bitArray.Length > 0 && bitArray.Length % 8 == 0 ? bitArray.Length / 8 : 0)];
            if (byteArray.Length == 0)
                return bitArray;

            bitArray.CopyTo(byteArray, 0);
            byte[] orderedByteArray = new byte[byteArray.Length];
            for (int k = 0; k < byteArray.Length; k++)
                orderedByteArray[k] = byteArray[byteArray.Length - 1 - k];

            return new BitArray(orderedByteArray);
        }

        /// <summary>
        /// Removes the parity bit from the sequence
        /// </summary>
        /// <param name="bitArray">BitArray to add 1 bit at start of</param>
        /// <returns></returns>
        public static BitArray RemoveMostImportantBit(BitArray bitArray)
        {
            BitArray newBitArray = new BitArray(bitArray.Length - 1);
            for (int k = 0; k < bitArray.Length - 1; k++)
                newBitArray.Set(k, bitArray[k + 1]);

            return newBitArray;
        }
        /// <summary>
        /// Checks first bit of bit array, returns false if it is big endian, true if it is little endian
        /// </summary>
        /// <param name="bitArray">BitArray to  check</param>
        /// <returns></returns>
        public static Boolean CheckMostImportantBit(BitArray bitArray)
        {
            return bitArray[0];
        }
        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }

        /// <summary>
        /// Add big or little endian parity bit at start of bitarray
        /// </summary>
        /// <param name="bitArray">BitArray to add 1 bit at start of</param>
        /// <param name="isBigEndian">Is it a big endian bit we are adding?</param>
        /// <returns></returns>
        public static BitArray SetMostImportantBit(BitArray bitArray, bool isBigEndian)
        {
            BitArray newBitArray = new BitArray(bitArray.Length + 1);
            newBitArray.Set(0, isBigEndian);
            for (int k = 0; k < bitArray.Length; k++)
                newBitArray.Set(k + 1, bitArray[k]);

            return newBitArray;
        }

        /// <summary>
        /// Used to transform an int array to string
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static string IntArrayToString(int[] arr)
        {
            List<char> x = new List<char>();
            foreach (int y in arr)
                x.Add((char)y);

            return new string(x.ToArray());
        }

        /// <summary>
        /// Used to convert any string to int array, that can then be converted based on big/little endianness
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int[] StringToIntArray(string value)
        {
            char[] x = value.ToCharArray();
            List<int> intArray = new List<int>();

            foreach (char c in x)
                intArray.Add((int)c);

            return intArray.ToArray();
        }

        public static int[] ReverseIntArray(int[] array, bool ToBigEndian)
        {
            List<int> newArray = new List<int>();
            foreach (int x in array)
                newArray.Add((ToBigEndian ? IPAddress.HostToNetworkOrder(x) : IPAddress.NetworkToHostOrder(x)));


            return newArray.ToArray();
        }

        public static void PrintArray(int[] arr)
        {
            Console.WriteLine("\n=== PRINTING ARRAY ===\n");
            foreach (int x in arr)
                Console.Write(x + " ");

            Console.WriteLine("\n=== FINISHED ===\n");
        }

        static string IntToBinaryString(int v)
        {
            string s = Convert.ToString(v, 2); // base 2
            string t = s.PadLeft(32, '0'); // add leading 0s
            string res = ""; // result
            for (int i = 0; i < t.Length; ++i)
            {
                if (i > 0 && i % 8 == 0)
                    res += " "; // add spaces
                res += t[i];
            }
            return res;
        }
        /// <summary>
        /// Used to determine what variable type is inside of a List of objects (which is made of all the variables obtained from parsing the data received in bits)
        /// </summary>
        /// <param name="x">The object's GET TYPE</param>
        /// <returns></returns>
        public static byte GetVariableId(string type)
        {
            byte b = 0;
            switch (type)
            {
                case "System.Boolean":
                    b = 1;
                    break;
                case "System.String":
                    b = 2;
                    break;
                case "System.Integer":
                    b = 3;
                    break;
            }
            return b;
        }
        // END
    }
}
