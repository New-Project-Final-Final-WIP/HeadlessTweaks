using System;
using System.Threading.Tasks;
using SkyFrost.Base;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        public partial class Commands
        {

            // Set permission level for a user
            // Usage: /setPerm [user id] [level]
            // level is PermissionLevel enum or int value
            // User can not set their own permission level
            // Target permission must be lower than or equal to your own

            [Command("setPerm", "Sets a user's permission level", "Moderation", PermissionLevel.Moderator, usage: "[user] [level]")]
            public async static Task SetPerm(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    _ = userMessages.SendTextMessage("Usage: /setperm [user] [level]");
                    return;
                }
                string user = args[0];
                string level = args[1];

                string userId = await TryGetUserId(user);
                if(userId == null)
                {
                    _ = userMessages.SendTextMessage($"Could not find user {user}");
                    return;
                }


                if (userId == msg.SenderId)
                {
                    _ = userMessages.SendTextMessage("You can not set your own permission level");
                    return;
                }
                else if (GetUserPermissionLevel(userId) > GetUserPermissionLevel(msg.SenderId))
                {
                    _ = userMessages.SendTextMessage("You can not set a user's permission level who is higher than you");
                    return;
                }

                // less < greater

                if (Enum.TryParse(level, true, out PermissionLevel levelEnum))
                {
                    // check if level is higher than your own
                    if (GetUserPermissionLevel(msg.SenderId) < levelEnum)
                    {
                        _ = userMessages.SendTextMessage("You can not set a user's permission level higher than yours");
                        return;
                    }
                    var levels = HeadlessTweaks.PermissionLevels.GetValue();
                    levels.Add(userId, levelEnum);
                    HeadlessTweaks.PermissionLevels.SetValueAndSave(levels);

                    _ = userMessages.SendTextMessage("Permission level set to " + levelEnum);
                }
                else
                {
                    _ = userMessages.SendTextMessage($"Invalid permission level \"{level}\"");
                }
            }

            // Get user permission level
            // Usage: /getPerm [?user id]

            [Command("getPerm", "Get user permission level", "Moderation", PermissionLevel.Moderator, usage: "[?user]")]
            public async static Task GetPerm(UserMessages userMessages, Message msg, string[] args)
            {
                string userId = msg.SenderId;
                if (args.Length >= 1)
                {
                    userId = await TryGetUserId(args[0]);
                    if (userId == null)
                    {
                        _ = userMessages.SendTextMessage($"Could not find user \"{args[0]}\"");
                        return;
                    }
                }
                _ = userMessages.SendTextMessage($"{userId} has a permission level of {GetUserPermissionLevel(userId)}");
            }
        }
    }
}
