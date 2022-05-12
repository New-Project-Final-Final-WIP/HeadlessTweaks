using System;
using CloudX.Shared;

using static CloudX.Shared.MessageManager;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        partial class Commands
        {

            // Set permission level for a user
            // Usage: /setPerm [user id] [level]
            // level is PermissionLevel enum or int value
            // User can not set their own permission level
            // Target permission must be lower than or equal to your own

            [Command("setPerm", "Sets a user's permission level", PermissionLevel.Moderator)]
            public static void SetPerm(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    userMessages.SendTextMessage("Usage: /setperm [user id] [level]");
                    return;
                }
                string userId = args[0];
                string level = args[1];

                if (userId == msg.SenderId)
                {
                    userMessages.SendTextMessage("You can not set your own permission level");
                    return;
                }
                else if (!userId.ToLower().StartsWith("u-"))
                {
                    userMessages.SendTextMessage($"'{userId}' is not a valid user id");
                    return;
                }
                else if (GetUserPermissionLevel(userId) > GetUserPermissionLevel(msg.SenderId))
                {
                    userMessages.SendTextMessage("You can not set a user's permission level who is higher than you");
                    return;
                }

                // less < greater

                if (Enum.TryParse(level, true, out PermissionLevel levelEnum))
                {
                    // check if level is higher than your own
                    if (GetUserPermissionLevel(msg.SenderId) < levelEnum)
                    {
                        userMessages.SendTextMessage("You can not set a user's permission level higher than yours");
                        return;
                    }
                    var levels = HeadlessTweaks.PermissionLevels.GetValue();
                    levels[userId] = levelEnum;
                    HeadlessTweaks.PermissionLevels.SetValueAndSave(levels);

                    userMessages.SendTextMessage("Permission level set to " + levelEnum);
                }
                else
                {
                    userMessages.SendTextMessage("Invalid permission level");
                }
            }

            // Get user permission level
            // Usage: /getPerm [User ID]

            [Command("getPerm", "Get user permission level", PermissionLevel.Moderator)]
            public static void GetPerm(UserMessages userMessages, Message msg, string[] args)
            {
                var userId = msg.SenderId;
                if (args.Length >= 1)
                {
                    userId = args[0];
                    if (!userId.ToLower().StartsWith("u-"))
                    {
                        userMessages.SendTextMessage($"'{userId}' is not a valid user id");
                        return;
                    }
                }
                userMessages.SendTextMessage($"{userId} has a permission level of {GetUserPermissionLevel(userId)}");
            }
        }
    }
}
