using FrooxEngine;
using System.Linq;
using SkyFrost.Base;
using System.Reflection;
using System.Threading.Tasks;
using System;
using System.Diagnostics;

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
                    if (!_RegisteredCommands.ContainsKey(commandStr.ToLower()))
                    {
                        userMessages.SendTextMessage($"Command '{commandStr}' not found");
                        return;
                    }
                    var method = _RegisteredCommands[commandStr.ToLower()];

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
                    _ = messages.Send();
                    return;
                }





                //var messages = new BatchMessageHelper(userMessages);

                // Iterate over all commands and print them
                var commandList = _RegisteredCommands.ToList();

                // Ignore aliases defined in the CommandAttribute
                commandList.RemoveAll(x => !(x.Value.GetCustomAttribute<CommandAttribute>()?.Name).Equals(x.Key, StringComparison.CurrentCultureIgnoreCase));


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
                
                _ = messages.Send();
            }

            // Toggle Opt out of auto-invites
            // Usage: /optOut

            [Command("optOut", "Toggles opt out of auto-invites", "Common")]
            public static void OptOut(UserMessages userMessages, Message msg, string[] args)
            {
                var optOut = HeadlessTweaks.AutoInviteOptOutList.GetValue();
                if (optOut.Contains(msg.SenderId))
                {
                    optOut.Remove(msg.SenderId);
                    _ = userMessages.SendTextMessage("Opted into auto-invites");
                }
                else
                {
                    optOut.Add(msg.SenderId);
                    _ = userMessages.SendTextMessage("Opted out of auto-invites");
                }
                HeadlessTweaks.AutoInviteOptOutList.SetValueAndSave(optOut);
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

            [Command("reqInvite", "Requests an invite to a world", "Common", PermissionLevel.None, aliases: ["requestInvite"], usage: "[?world name...]")]
            public static void ReqInvite(UserMessages userMessages, Message msg, string[] args)
            {
                World world = null;
                if (args.Length < 1)
                {
                    world = Engine.Current.WorldManager.FocusedWorld;
                    goto Invite;

                }

                string worldName = string.Join(" ", args).Trim();

                var worlds = Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld);

                world = worlds.Where(w => w.RawName == worldName || w.SessionId == worldName).FirstOrDefault();
                if (world == null)
                {
                    if (int.TryParse(worldName, out var result))
                    {
                        var worldList = worlds.ToList();
                        if (result < 0 || result >= worldList.Count)
                        {
                            _ = userMessages.SendTextMessage($"World index {result} out of range");
                            return;
                        }
                        world = worldList[result];
                    }
                    else
                    {
                        _ = userMessages.SendTextMessage($"No world found with the name \"{world.Name}\"");
                        return;
                    }
                }

            Invite:
                // check if user can join world
                if (!CanUserJoin(world, msg.SenderId))
                {
                    _ = userMessages.SendTextMessage($"You can't join world \"{world.Name}\"");
                    return;
                }
                world.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(world.GenerateSessionInfo());
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
                    _ = userMessages.SendTextMessage($"You can't join world \"{world.Name}\"");
                    return;
                }

                _ = userMessages.SendTextMessage($"Getting world orb for \"{world.Name}\"");
                world.RunSynchronously(async () =>
                {
                    var orb = world.GetOrb(true);
                    var a = await userMessages.SendObjectMessage(orb, OfficialAssets.Graphics.Icons.Dash.Worlds);
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
                _ = messages.Send();
            }

            // Shutdown Headless
            [Command("shutdown", "Shutdown Headless", "Headless Management", PermissionLevel.Owner)]
            public static async Task ShutDown(UserMessages userMessages, Message msg, string[] args)
            {
                await userMessages.SendTextMessage($"Shutting down Headless");

                string sender = msg.SenderId;

                var senderContact = Engine.Current.Cloud.Contacts.GetContact(msg.SenderId);
                if(senderContact != null) {
                    sender = senderContact.ContactUsername;
                }

                HeadlessTweaks.Warn($"Shutdown initiated via command from user {sender}");
                

                var headlessProgram = Type.GetType("FrooxEngine.Headless.Program, Resonite");
                var shutdownMethod = headlessProgram?.GetMethod("Shutdown", BindingFlags.NonPublic | BindingFlags.Static);

                if(shutdownMethod != null)
                {
                    await (Task)shutdownMethod.Invoke(null, null);
                    Process.GetCurrentProcess().Kill(); // Process does not fully end without interacting with the terminal, so lets force it
                    return;
                }
                HeadlessTweaks.Error($"Could not find headless shut down method: \n\t\tProgram type: {headlessProgram}\n\t\tShutdown Method: {shutdownMethod}");
                await userMessages.SendTextMessage($"Could not find headless specific shutdown method\nDefaulting to Userspace shutdown");

                Userspace.ExitApp(false);
            }

            // Throw an error
            // Usage: /throwErr

            [Command("throwErr", "Throw Error", "Debug", PermissionLevel.Owner)]
            public static void ThrowError(UserMessages userMessages, Message msg, string[] args)
            {
                throw new Exception("Throw Error test command");
            }

            
            // Throw an error asynchronously
            // Usage: /throwErrAsync

            [Command("throwErrAsync", "Throw Error Asynchronously", "Debug", PermissionLevel.Owner)]
            public static async Task ThrowErrorAsync(UserMessages userMessages, Message msg, string[] args)
            {
                await Task.CompletedTask;
                throw new Exception("Async Throw Error test command");
            }
        }
    }
}
