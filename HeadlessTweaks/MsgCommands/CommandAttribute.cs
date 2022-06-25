using System;

namespace HeadlessTweaks
{
    public partial class MessageCommands
    {
        // Command Attributes
        // Name: The name of the command
        // Description: The description of the command
        // Category: The category of the command
        // PermissionLevel: The permission level required to use the command
        // Alliases: The aliases of the command
        // Usage: The arguments of the command

        public class CommandAttribute : Attribute
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public PermissionLevel PermissionLevel { get; set; }
            public string[] Aliases { get; set; }
            public string Usage { get; set; }


            public CommandAttribute(string name, string description, string category, PermissionLevel permissionLevel = PermissionLevel.None, string usage = null, params string[] aliases)
            {
                Name = name;
                Description = description;
                Category = category;
                PermissionLevel = permissionLevel;
                Aliases = aliases;
                Usage = usage;
            }
        }
    }
}
