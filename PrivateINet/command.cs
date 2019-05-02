using System;
using System.Collections.Generic;
using Hub;
using System.Collections;
using System.Linq;

namespace PrivateINet
{
    class command
    {
        public string commandName { get; set; }
        public int[] argumentLengths { get; set; }//counter for number of bits making up each argument
        public BitArray arguments { get; set; }
        //revise, add enum
        public byte[] argumentTypes { get; set; } //helps parsing argument data into correct variable types. 1 is boolean, 2 is string 3 is integer
        public string description { get; set; }
        /// <summary>
        /// Used to set arguments of a command
        /// </summary>
        /// <param name="arguments">List of variables to pass as arguments</param>
        public void setArguments(List<object> variables)
        {
            int counter = 0;
            List<byte> varsToBytes = new List<byte>();
            foreach (object variable in variables)
            {
                if (counter >= argumentTypes.Length)
                {
                    Console.WriteLine("Arguments provided for command are more than the expected variable types. Please check if hte command is valid first. Reffer to checkMatchingArgumentFormat().\nMeantime the first " + counter + " variable/s has/have been set as arguments of the command.");
                    break;
                }
                byte[] tmpBytes = utilities.ObjectToByteArray(variable, argumentTypes[counter++]);
                varsToBytes.AddRange(tmpBytes);
            }
            //set arguments themselves
            arguments = new BitArray(varsToBytes.ToArray());
        }
        /// <summary>
        /// Used to get arguments in List<object> format
        /// </summary>
        /// <returns></returns>
        public List<object> getArguments()
        {
            return utilities.BitArrayToListObject(arguments, argumentLengths, argumentTypes);
        }

        /// <summary>
        /// Used to check if command has correctly formatted arguments as what is expected
        /// </summary>
        /// <param name="commandID">Command to check expected argument lengths with</param>
        /// <returns></returns>
        public errorInArgument checkMatchingArgumentFormat(int commandID)
        {
            //check existing command
            if (Program.db.commands.Where(c => c.Key == commandID).Count() == 0)//no such command found
                return new errorInArgument
                {
                    details = "No command with ID: " + commandID + " found.",
                    valid = false
                };

            command arg = Program.db.commands.Where(c => c.Key == commandID).Select(a => a.Value).FirstOrDefault();
            int totalLengthOfExpectedArguments = arg.argumentLengths.Sum();
            //check  argument length is as expected
            //if number of bits of data minus expected total number of bits does snot equal to 0 then length of data is not as expected
            if (arguments.Length - totalLengthOfExpectedArguments != 0)
                return new errorInArgument
                {
                    details = "Bitarray of arguments is not of expected length. Expected: " + totalLengthOfExpectedArguments + ", actual: " + arguments.Length + ".",
                    valid = false
                };

            return new errorInArgument
            {
                details = "Argument seems to be alright.",
                valid = true
            }; ;
        }
    }

    class errorInArgument
    {
        public string details { get; set; } //what is the issue?
        public bool valid { get; set; }//any argument not valid should be fixed!
    }
}
