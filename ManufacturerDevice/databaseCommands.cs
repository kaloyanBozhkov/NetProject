using System.Collections.Generic;
using System.Text;
using System.Collections;
namespace ManufacturerDevice
{
    /// <summary>
    /// This is a simple "database" of all the commands that this Manufacturer Device can accept and act to
    /// For argument types: 1 is boolean, 2 is string 3 is integer
    /// </summary>
    class databaseCommands
    {
        /// <summary>
        /// Int is the ID of the command, argument is the argument obj.
        /// A command's Type is the only thing that should be set properly. 
        /// Using command.setArgument will update and set argumentLengths and arguments accordingly.
        /// </summary>
        public Dictionary<int, command> commands = new Dictionary<int, command>() {
            {
                /// <summary>
                /// Argument length expected to be 1 bit, dedicated to true or false for reboot value
                /// </summary>
                1,
                new command()
                {
                    commandName = "Reboot",
                    argumentLengths =  new int[] { 1 },
                    argumentTypes = new byte[] { 1 },
                    arguments = new BitArray(1), //1 or 0, 1 is true
                    description = "boolean variable, representative of reboot or not"
                }
            },
            {
                /// <summary>
                /// Argument expected to be the string 'hello world'
                /// </summary>
                420,
                new command()
                {
                    commandName = "Hello World",
                    argumentLengths = new int[] { 88 },
                    argumentTypes = new byte[] { 2 },
                    arguments = new BitArray(ASCIIEncoding.ASCII.GetBytes("hello world")),
                    description = "Test command to send a single string with 11 characters, such as hello world"
                }
            },
            {
                /// <summary>
                /// Argument expected to be a string of any 11 characters
                /// </summary>
                350,
                new command()
                {
                    commandName = "Some String",
                    argumentLengths = new int[] { 88 },
                    argumentTypes = new byte[] { 2 },
                    arguments = new BitArray(0),
                    description = "The arguments of this command is a word of up to 100 characters"
                }
            },
            {
                /// <summary>
                /// Argument expected to be a boolean for reboot (true) and an 32 bits for the number of milliseconds after which to perform reboot
                /// </summary>
                100,
                new command()
                {
                    commandName = "Timed Reboot",
                    argumentLengths = new int[] { 1, 32 },
                    argumentTypes = new byte[] { 1, 4 }, //1 is boolean 4 is int
                    arguments = new BitArray(0),
                    description = "The arguments of this command is a word of up to 100 characters"
                }
            },
            {
                /// <summary>
                /// Argument expected to be a string of any 100 characters and an integer
                /// </summary>
                619,
                new command()
                {
                    commandName = "Some String and some integer",
                    argumentLengths = new int[] { 800 , 32 },
                    argumentTypes = new byte[] { 2 , 3},
                    arguments = new BitArray(0),
                    description = "The arguments of this command is a word of up to 100 characters"
                }
            }
        };
    }
}
