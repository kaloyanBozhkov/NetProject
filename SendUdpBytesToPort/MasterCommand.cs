using System;
using System.Collections.Generic;
using System.Collections;
namespace SendUdpBytesToPort
{
    /// <summary>
    /// JOSH you use this obj structure when sending JSON commands over to me 
    /// </summary>
    class MasterCommand
    {
        /// <summary>
        /// SEND )to send a command), MODIFY (to update or delete db command) or REQUEST
        /// </summary>
        public string commandType { get; set; } = "";

        //START UPDATE SPECIFIC SETTINGS 
        public string destinationMAC { get; set; } = "";
        //command from db of commands
        public string commandName { get; set; } = "";
        //command ID from db of commands. If command ID is null then command name is used to fetch command from DB
        //send it as string, empty if no command ID, due to JSON parser on receiver end
        public string commandId { get; set; } = "";

        //argument lengths and types are SAME as on database's command. Here it is list of bool isntead of bitarray due to JSON parser requirements
        public List<bool> arguments { get; set; } = new List<bool> { };
        //END UPDATE SPECIFIC SETTINGS


        //START MODIFY SPECIFIC SETTINGS
        //using commandId, commandName, arguments from SEND command's specific settings
        public int[] argumentLengths { get; set; } = new int[0];
        //argument types: 1 is boolean, 2 is string 3 is integer
        //here they are declared as integer array due to JSON limited variable types. Once i receive it it will be parsed to byte and work accordingly
        public int[] argumentTypes { get; set; } = new int[0];
        public string description { get; set; } = "";
        //if true then MODIFY is used to modify database by adding a NEW command instead of editing an existing one
        public bool createNewCommand { get; set; } = false;
        //END MODIFY SPECIFIC SETTINGS

        //START REQIEST SPECIFIC SETTINGS
        public bool deleteRequestedCommand { get; set; } = false;
        //END REQUEST SPECIFIC SETTINGS
    }
}
