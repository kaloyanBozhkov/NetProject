using System;
using System.Collections.Generic;
using System.Collections;
namespace PrivateINet
{
    /// <summary>
    /// JOSH you use this obj structure when sending JSON commands over to me 
    /// </summary>
    class MasterCommand
    {
        /// <summary>
        /// SEND, MODIFY or REQUEST
        /// </summary>
        public string commandType { get; set; } = "";


        //START UPDATE SPECIFIC SETTINGS 
        public string destinationMAC { get; set; } = "";
        //command from db of commands
        public string commandName { get; set; } = "";
        //command ID from db of commands. If command ID is null then command name is used to fetch command from DB
        public string commandId { get; set; } = "";

        //BE SURE TO HAVE SAME ARGUMENT LENGTHS AND TYPES AS ONES EXPECTED ON DB
        //argument lengths and types are SAME as on database's command. Here it is list of bool isntead of bitarray due to JSON parser requirements
        public List<bool> arguments { get; set; } = new List<bool> { false };

        public BitArray getArgumentsAsBitArray()
        {
            BitArray argumentsAsBitArray = new BitArray(arguments.Count);
            int count = 0;
            foreach (bool x in arguments)
                argumentsAsBitArray.Set(count++, x);

            return argumentsAsBitArray;
        }
        //END UPDATE SPECIFIC SETTINGS



        //START MODIFY SPECIFIC SETTINGS
        //using commandId, commandName, arguments from SEND command's specific settings
        public int[] argumentLengths { get; set; } = new int[0];
        public int[] argumentTypes { get; set; } = new int[0];
        public byte[] getArgumentTypesAsByteArray()
        {
            return Array.ConvertAll(argumentTypes, c => (byte)c);
        }
        public string description { get; set; } = "";
        //if true then MODIFY is used to modify database by adding a NEW command instead of editing an existing one
        public bool createNewCommand { get; set; } = false;
        //END MODIFY SPECIFIC SETTINGS

        /*START REQUEST SPECIFIC SETTINGS*/
        public bool deleteRequestedCommand { get; set; } = false;
        /*END REQUEST SPECIFIC SETTINGS*/
    }
}
