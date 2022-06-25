using FrooxEngine;
using System.Linq;
using CloudX.Shared;
using System.Reflection;

using static CloudX.Shared.MessageManager;

namespace HeadlessTweaks
{

    partial class MessageCommands
    {
        public partial class Commands
        {
            // Show help
            // Usage: /help [?command]
            // If no command is given show all commands

            [Command("help", "Shows this help message", "Common", usage: "[?command]")]
            public static void Help(UserMessages userMessages, Message msg, string[] args)
            {
                var messages = new BatchMessageHelper(userMessages);
                // Check if command arg is given
                if (args.Length > 0)
                {
                    var commandStr = args[0];
                    // Check if command exists
                    if (!commands.ContainsKey(commandStr.ToLower()))
                    {
                        userMessages.SendTextMessage($"Command '{commandStr}' not found");
                        return;
                    }
                    var method = commands[commandStr.ToLower()];

                    var attr = method.GetCustomAttribute<CommandAttribute>();

                    if (GetUserPermissionLevel(msg.SenderId) < attr.PermissionLevel)
                    {
                        userMessages.SendTextMessage($"Command '{commandStr}' not found");
                        return;
                    };
                    
                    /* Message to return:
                     * 
                     * /name usage
                     * description
                     * category
                     * aliases
                     */

                    // Command 
                    messages.Add($"/{attr.Name} {attr.Usage}");
                    messages.Add(attr.Description);

                    messages.Add("Category: " + attr.Category);
                    
                    if (attr.Aliases.Length > 0)
                    {
                        messages.Add("Aliases:");
                        foreach (var alias in attr.Aliases)
                        {
                            messages.Add($"/{alias}", true);
                        }
                    }
                    messages.Send();
                    return;
                }





                //var messages = new BatchMessageHelper(userMessages);

                // Iterate over all commands and print them
                var commandList = commands.ToList();

                // Ignore aliases defined in the CommandAttribute
                commandList.RemoveAll(x => x.Value.GetCustomAttribute<CommandAttribute>()?.Name.ToLower() != x.Key.ToLower());


                foreach (var command in commandList)
                {
                    var method = command.Value;
                    var attr = method.GetCustomAttribute<CommandAttribute>();
                    if (attr != null)
                    {
                        // skip if permission level is higher than the user
                        if (GetUserPermissionLevel(msg.SenderId) < attr.PermissionLevel) continue;

                        var message = $"{attr.Name} - {attr.Description}";

                        // if there are aliases, print them too
                        if (attr.Aliases.Length > 0)
                        {
                            message += $"\nAliases: {string.Join(", ", attr.Aliases)}";
                        }

                        messages.Add(message, true);
                    }
                }
                
                messages.Send();
            }

            // Toggle Opt out of auto-invites
            // Usage: /optOut

            [Command("optOut", "Toggles opt out of auto-invites", "Common")]
            public static void OptOut(UserMessages userMessages, Message msg, string[] args)
            {
                var optOut = HeadlessTweaks.config.GetValue(HeadlessTweaks.AutoInviteOptOut);
                if (optOut.Contains(msg.SenderId))
                {
                    optOut.Remove(msg.SenderId);
                    _ = userMessages.SendTextMessage("Opted in to auto-invites");
                }
                else
                {
                    optOut.Add(msg.SenderId);
                    _ = userMessages.SendTextMessage("Opted out of auto-invites");
                }
                HeadlessTweaks.config.Set(HeadlessTweaks.AutoInviteOptOut, optOut);
                HeadlessTweaks.config.Save();
            }

            // Mark all as read
            // Usage: /markAllRead

            [Command("markAllRead", "Marks all messages as read", "Common")]
            public static void MarkAllRead(UserMessages userMessages, Message msg, string[] args)
            {
                userMessages.MarkAllRead();
            }

            // Invite me to a specific world by name or to the current world if no name is given
            // Usage: /reqInvite [?world name...]

            [Command("reqInvite", "Requests an invite to a world", "Common", PermissionLevel.None, usage: "[?world name...]", "requestInvite")]
            public static void ReqInvite(UserMessages userMessages, Message msg, string[] args)
            {
                World world = null;
                if (args.Length < 1)
                {
                    world = Engine.Current.WorldManager.FocusedWorld;
                    goto Invite;

                }
                string worldName = string.Join(" ", args);
                worldName.Trim();
                var worlds = Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld);

                world = worlds.Where(w => w.RawName == worldName || w.SessionId == worldName).FirstOrDefault();
                if (world == null)
                {
                    if (int.TryParse(worldName, out var result))
                    {
                        var worldList = worlds.ToList();
                        if (result < 0 || result >= worldList.Count)
                        {
                            _ = userMessages.SendTextMessage("World index out of range");
                            return;
                        }
                        world = worldList[result];
                    }
                    else
                    {
                        _ = userMessages.SendTextMessage("No world found with the name " + worldName);
                        return;
                    }
                }

            Invite:
                // check if user can join world
                if (!CanUserJoin(world, msg.SenderId))
                {
                    _ = userMessages.SendTextMessage("You can't join " + world.Name);
                    return;
                }
                world.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(world.GetSessionInfo());
            }

            // Get session orb
            // Usage: /getSessionOrb [?world name...]
            // If no world name is given, it will get the session orb of the user's world

            [Command("getSessionOrb", "Get session orb", "Common", usage: "[?world name...]")]
            public static void GetSessionOrb(UserMessages userMessages, Message msg, string[] args)
            {
                // Get world by name or user world
                World world = GetWorldOrUserWorld(userMessages, string.Join(" ", args), msg.SenderId, true);
                if (world == null) return;

                // check if user can join world
                if (!CanUserJoin(world, msg.SenderId))
                {
                    _ = userMessages.SendTextMessage("You can't join " + world.Name);
                    return;
                }



                world.RunSynchronously(async () =>
                {
                    var orb = world.GetOrb(true);
                    var a = await userMessages.SendObjectMessage(orb);
                    if (a) world.AllowUserToJoin(msg.SenderId);
                });
            }

            // List worlds
            // Usage: /worlds

            [Command("worlds", "List all worlds", "Common")]
            public static void Worlds(UserMessages userMessages, Message msg, string[] args)
            {
                var messages = new BatchMessageHelper(userMessages);
                int num = 0;
                foreach (World world1 in Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld && CanUserJoin(w, msg.SenderId)))
                {
                    messages.Add($"[{num}] {world1.Name} | {world1.ActiveUserCount} ({world1.UserCount}) | {world1.AccessLevel}", true);
                    ++num;
                }
                messages.Send();
            }


            // Throw an error
            // Usage: /throwErr

            [Command("throwErr", "Throw Error", "Debug", PermissionLevel.Owner)]
            public static void ThrowError(UserMessages userMessages, Message msg, string[] args)
            {
                throw new System.Exception("Error Thrown");
            }

            
            // Throw an error asynchronously
            // Usage: /throwErrAsync

            [Command("throwErrAsync", "Throw Error Asynchronously", "Debug", PermissionLevel.Owner)]
            public static async System.Threading.Tasks.Task ThrowErrorAsync(UserMessages userMessages, Message msg, string[] args)
            {
                throw new System.Exception("Async Error Thrown");
            }
        }
    }
}
