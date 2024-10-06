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

        [AttributeUsage(AttributeTargets.Method)]
        public class CommandAttribute(string name, string description, string category, PermissionLevel permissionLevel = PermissionLevel.None, string usage = null, params string[] aliases) : Attribute
        {
            public string Name { get; set; } = name;
            public string Description { get; set; } = description;
            public string Category { get; set; } = category;
            public PermissionLevel PermissionLevel { get; set; } = permissionLevel;
            public string[] Aliases { get; set; } = aliases;
            public string Usage { get; set; } = usage;
        }
    }
}
