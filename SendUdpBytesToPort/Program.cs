using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Json.Net;
using System.Collections;

namespace SendUdpBytesToPort
{
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
    /// <summary>
    /// This is the returned response to
    /// </summary>
    class ResponseLocalhost
    {
        public bool success { get; set; }
        public string information { get; set; }
        public string commandType { get; set; }
    }

    /// <summary>
    /// command object, to which a requested command received as JSON string is parsed to
    /// </summary>
    class command
    {
        public string commandName { get; set; }
        public int[] argumentLengths { get; set; }//counter for number of bits making up each argument
        public BitArray arguments { get; set; }
        public byte[] argumentTypes { get; set; } //helps parsing argument data into correct variable types. 1 is boolean, 2 is string 3 is integer
        public string description { get; set; }
    }
   class Program
    {
        //this is used for SEND > NO: when wanting to send command with new argument values instead of default ones found on db.
        public static bool requestForSend { get; set; } = false;
       

    static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("What Master Command to send to Koko? (SEND, MODIFY, REQUEST)");
                string input = Console.ReadLine();
                if (input.ToLower() != "exit" && (input.ToLower() == "send" || input.ToLower() == "modify" || input.ToLower() == "request"))
                {
                    MasterCommand MS = new MasterCommand();
                    commandTypeExamples(input, ref MS);

                    //any command object sent must be stringified to JSON first, the string then is sent as bytes
                    string jsonString = Jil.JSON.Serialize(MS);
                    Console.WriteLine("Master Command serialized to JSON string: \n" + jsonString);
                    //Send any byte array through local host like i do here
                    Send(ASCIIEncoding.ASCII.GetBytes(jsonString));
                    if(!requestForSend)
                        Console.WriteLine("\n\nUDP packet with Master Command has been sent to Koko through localhost!\n\n");

                    ListenForResponse();
                }
                else
                {
                    break;
                }
            }
        }

        static void commandTypeExamples(string commandType, ref MasterCommand MS)
        {
            if(commandType.ToUpper() == "MODIFY")
            {
                MS.commandType = "MODIFY";
                
                Console.WriteLine("Do you wnat to update or create a new command?");
                if(Console.ReadLine().ToUpper() == "CREATE")
                {
                    MS.createNewCommand = true;
                    Console.WriteLine("\nChoose a command ID:");
                    MS.commandId = Console.ReadLine();
                    Console.WriteLine("\nCommand name:");
                    MS.commandName = Console.ReadLine();
                    Console.WriteLine("\nCommand description:");
                    MS.description = Console.ReadLine();
                    Console.WriteLine("\nSet argument lengths:");
                }
                else
                {
                    MS.createNewCommand = false;
                    Console.WriteLine("\nChoose a command ID of an existing command to modify: \n");
                    MS.commandId = Console.ReadLine();
                    Console.WriteLine("\nNew command name:");
                    MS.commandName = Console.ReadLine();
                    Console.WriteLine("\nNew command description:");
                    MS.description = Console.ReadLine();
                    Console.WriteLine("\nNew argument lengths:");
                }
                List<int> argLenghts = new List<int>();
                List<int> argTypes = new List<int>();
                while (true)
                {
                    int tmpInt = 1;
                    int tmpArgtype = 1;

                    Console.WriteLine("\nWhat type is argument n. " + (argTypes.Count + 1) + "?\n1 - Boolean\n2 - String\n3 - Integer\n");
                    int.TryParse(Console.ReadLine(), out tmpArgtype);
                    argTypes.Add(tmpArgtype);

                    if(tmpArgtype == 2)
                    {
                        Console.WriteLine("\nSince argument n. " + (argLenghts.Count + 1) + " is of type " + getTypeFromInt(tmpArgtype) + ", you must specify its length now. Do so by value or length? (value/length)\n");
                        while (true)
                        {
                            string input = Console.ReadLine().ToLower();
                            if (input == "value")
                            {
                                Console.WriteLine("\nWhat is the string based on which the number of bits will be determined?\n");
                                //adding bits for argument which is a string
                                //When passing a string and you're counting its bits, remember it is ASCII encoding!!!!
                                string argumentStringToPass;
                                while (true)
                                {
                                    argumentStringToPass = Console.ReadLine();
                                    if (ASCIIEncoding.ASCII.GetBytes(argumentStringToPass).Length > 0)
                                        break;
                                    else
                                        Console.WriteLine("\nThe string must be of at least 1 valid ASCII character\n");
                                }
                                tmpInt = argumentStringToPass.Length * 8;
                                break;
                            }
                            else if(input == "length")
                            {
                                Console.WriteLine("\nHow many bits should the string be of?\n");
                                while (true)
                                    if(int.TryParse(Console.ReadLine(), out tmpInt) && tmpInt > 0)
                                        break;
                                    else
                                        Console.WriteLine("\nTry with a proper int value, also greater than 0\n");

                                break;
                            }
                            else
                            {
                                Console.WriteLine("\nChoose either length or value!\n");
                            }
                        }
                        
                    }
                    else
                    {
                        if(tmpArgtype == 1) //is boolean
                        {
                            tmpInt = 1;
                        }
                        else if (tmpArgtype == 3) //is int
                        {
                            tmpInt = 32;
                        }
                    }

                    argLenghts.Add(tmpInt);
                    Console.WriteLine("\nAdd another argument? (yes/no)\n");
                    if (Console.ReadLine().ToLower() != "yes")
                        break;
                }
                MS.argumentLengths = argLenghts.ToArray();
                MS.argumentTypes = argTypes.ToArray();

                setArguments(ref MS);
            }
            else if (commandType.ToUpper() == "SEND")
            {
                Console.WriteLine("What command name/ command ID to execute from DB?");
                msSetupForSend(ref MS, Console.ReadLine());
                //argument lengths and types are SAME as on database's command.
                Console.WriteLine("Send command with default arguments as in DB? (yes)/(no) \n");
                if(Console.ReadLine().ToUpper() == "NO")
                {
                    requestForSend = true;
                    MS.commandType = "REQUEST";
                }
            }else if(commandType.ToUpper() == "REQUEST")
            {
                Console.WriteLine("What command name / command ID to request from DB?");
                string commandNameOrIdToSend = Console.ReadLine();
                MS.commandType = "REQUEST";
                //On my end I check first command name then command ID in DB so either is fine. 
                //Just make sure that if you send command ID through you use MS.commandId instead of commandName.
                int cmdId;
                bool isNumeric = int.TryParse(commandNameOrIdToSend, out cmdId);
                if (!isNumeric)
                {
                    MS.commandName = commandNameOrIdToSend;
                }
                else
                {
                    MS.commandId = commandNameOrIdToSend;
                }
                Console.WriteLine("DELETE command if it exists in DB? (yes)/(no)\n");
                MS.deleteRequestedCommand = Console.ReadLine().ToUpper() == "YES" ? true : false;
            }
        }

        static void Send(byte[] bytesOfMessage)
        {
            //make sure you are sending to these IP and port
            string localhost = "127.0.0.1";
            int port = 4343;
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse(localhost);
            IPEndPoint endPoint = new IPEndPoint(broadcast, port);
            s.SendTo(bytesOfMessage, endPoint);
            s.Close();
        }
       
        static void ListenForResponse()
        {
            //LOCALHOST LISTEN FOR COMMANDS
            string localhost = "128.0.0.1";
            int port = 4342;//respones are received on port 81, sent on 80
            UdpClient listener = new UdpClient(port);
            //Create IP end point to which to send command packets
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(localhost), port);
            try
            {
                while (true)
                {
                    /*HANDLER FOR JOSH*/
                    Console.WriteLine("Waiting for a message from Koko");
                    byte[] bytes = listener.Receive(ref endPoint);
                    Console.WriteLine("Received bytes from {0}\n", endPoint.ToString());
                    string jsonMasterCommand = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                    Console.WriteLine("Received Master Command as JSON string:\n{0}\n", jsonMasterCommand);

                    if (requestForSend)
                    {//after getting the command's arg length and types we can send a new command of that ID with custom arguments based on the commands expected arg lengths and types
                        requestForSend = false;
                        sendCommandNewArguments(Jil.JSON.Deserialize<ResponseLocalhost>(jsonMasterCommand));
                    }
                    break;
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
        }

        static void sendCommandNewArguments(ResponseLocalhost resp)
        {
            //parse json string containing MS related information
            commandFormattedForJSON cmd = Jil.JSON.Deserialize<commandFormattedForJSON>(resp.information);
            MasterCommand MS = new MasterCommand();
            MS.commandName = cmd.commandName.ToLower();
            MS.argumentLengths = cmd.argumentLengths;
            MS.argumentTypes = cmd.argumentTypes;
            msSetupForSend(ref MS, MS.commandName);
            setArguments(ref MS);

            //after creating the command to SEND (which has the new arguments) send it!

            //any command object sent must be stringified to JSON first, the string then is sent as bytes
            string jsonString = Jil.JSON.Serialize(MS);
            Console.WriteLine("Master Command serialized to JSON string: \n" + jsonString);
            //Send any byte array through local host like i do here

            Send(ASCIIEncoding.ASCII.GetBytes(jsonString));
            Console.WriteLine("\n\nUDP packet with Master Command has been sent to Koko through localhost!\n\n");
        }

        static void msSetupForSend(ref MasterCommand MS, string commandNameOrIdToSend)
        {
            MS.commandType = "SEND";
            //On my end I check first command name then command ID in DB so either is fine. 
            //Just make sure that if you send command ID through you use MS.commandId instead of commandName.

            int cmdId;
            bool isNumeric = int.TryParse(commandNameOrIdToSend, out cmdId);
            if (!isNumeric)
            {
                MS.commandName = commandNameOrIdToSend;
            }
            else
            {
                MS.commandId = commandNameOrIdToSend;
            }

            //my pc mac address format is checked on my end
            MS.destinationMAC = "4C:ED:FB:6A:6C:A2";
            // josh pc MS.destinationMAC = "D8:9D:67:D0:B2:69";
        }

        static string getTypeFromInt(int type)
        {
            string returnStr = "";
            switch (type)
            {
                case 1:
                    returnStr = "boolean";
                    break;
                case 2:
                    returnStr = "string";
                    break;
                case 3:
                    returnStr = "integer";
                    break;
            }
            return returnStr;
        }

        static void setArguments(ref MasterCommand MS)
        {
            List<bool> arugmentsAsListBool = new List<bool>();

            for (int k = 0; k < MS.argumentTypes.Length; k++)
            { //for this exmaple only int, stirng and boolean will be considered..
                Console.WriteLine("\nInsert value for argument n. " + (k + 1) + " of type " + getTypeFromInt(MS.argumentTypes[k]) + " with bit length of " + MS.argumentLengths[k] + ".\n");

                if (getTypeFromInt(MS.argumentTypes[k]) == "boolean")
                {
                    //Adding bit for argument which is a boolean
                    bool secondArgumentWhichIsABoolean = Console.ReadLine().ToLower() == "true" ? true : false;
                    arugmentsAsListBool.Add(secondArgumentWhichIsABoolean);
                }
                else if (getTypeFromInt(MS.argumentTypes[k]) == "string")
                {
                    //adding bits for argument which is a string
                    //When passing a string and you're counting its bits, remember it is ASCII encoding!!!!
           
                    string argumentStringToPass;
                    while (true)
                    {
                        argumentStringToPass = Console.ReadLine();
                        if (ASCIIEncoding.ASCII.GetBytes(argumentStringToPass).Length * 8 == MS.argumentLengths[k])
                            break;
                        else
                            Console.WriteLine("The string must be of {0} ASCII characters exactly! Current is {1}", MS.argumentLengths[k], argumentStringToPass.Length);
                    }

                    BitArray tba = new BitArray(ASCIIEncoding.ASCII.GetBytes(argumentStringToPass));
                    foreach (bool x in tba)
                        arugmentsAsListBool.Add(x);
                }
                else if (getTypeFromInt(MS.argumentTypes[k]) == "integer")
                {
                    int tmpInt = 0;
                    int.TryParse(Console.ReadLine(), out tmpInt);
                    BitArray tba = new BitArray(System.BitConverter.GetBytes(tmpInt));
                    foreach (bool x in tba)
                        arugmentsAsListBool.Add(x);
                }
            }

            MS.arguments = arugmentsAsListBool;
        }
    }
}
