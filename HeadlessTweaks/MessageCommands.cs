using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using CloudX.Shared;
using BaseX;

using static CloudX.Shared.MessageManager;

namespace HeadlessTweaks
{
    internal class MessageCommands
    {
        internal static void Init(Harmony harmony)
        {
            HeadlessTweaks.Msg("Initializing MessageCommands");

            var target = typeof(WorldStartSettingsExtensions).GetMethod("SetWorldParameters");
            var prefix = typeof(MessageCommands).GetMethod("Prefix");
            var postfix = typeof(MessageCommands).GetMethod("Postfix");

            harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

            Engine.Current.RunPostInit(HookIntoMessages);
        }

        private static void HookIntoMessages()
        {
            Engine.Current.Cloud.Messages.OnMessageReceived += OnMessageReceived;
        }

        private static void OnMessageReceived(Message obj)
        {
            Task.Run(async () =>
            {
                DateTime time = DateTime.UtcNow;
                var ids = new List<string> { obj.Id };

                await Engine.Current.Cloud.HubClient.MarkMessagesRead(new MarkReadBatch()
                {
                    SenderId = Engine.Current.Cloud.Messages.SendReadNotification ? obj.SenderId : null,
                    Ids = ids,
                    ReadTime = time
                }).ConfigureAwait(false);
            });
            switch (obj.MessageType)
            {
                case CloudX.Shared.MessageType.Text:
                    if (obj.Content.StartsWith("/"))
                    {
                        var userMessages = getUserMessages(obj.SenderId);
                        var args = obj.Content.Split(' ');
                        var cmd = args[0].Substring(1).ToLower();
                        var cmdArgs = args.Skip(1).ToArray();

                        var cmdMethod = typeof(Commands).GetMethod(cmd);

                        // Check if user has permission to use command
                        // CommandAttribute.PermissionLevel
                        var cmdAttr = cmdMethod.GetCustomAttribute<Commands.CommandAttribute>();
                        if (cmdAttr == null) return;

                        if (cmdAttr.PermissionLevel > getUserPermissionLevel(obj.SenderId))
                        {
                            userMessages.SendTextMessage("You do not have permission to use that command.");
                            return;
                        }
                        
                        if (cmdMethod == null)
                        {
                            userMessages.SendTextMessage("Unknown command");
                            return;
                        }
                        HeadlessTweaks.Msg("Executing command: " + cmd);
                        cmdMethod.Invoke(null, new object[] { userMessages, obj, cmdArgs });
                    }
                    return;
                default:
                    return;
            }
        }

        public static void Prefix(ref WorldStartupParameters info, out List<string> __state)
        {
            if (info.AutoInviteUsernames == null)
            {
                __state = null;
                return;
            }
            __state = new List<string>(info.AutoInviteUsernames);
            info.AutoInviteUsernames.Clear();
        }
        public static void Postfix(WorldStartupParameters info, List<string> __state, World world)
        {
            //AutoInviteOptOut
            if (__state == null || __state.Count <= 0)
                return;
            if (world.Engine.Cloud.CurrentUser == null)
            {
                UniLog.Log("Not logged in, cannot send auto-invites!", false);
                return;
            }

            if (__state == null) return;
            Task.Run(async () =>
            {
                foreach (string autoInviteUsername in __state)
                {
                    string username = autoInviteUsername;
                    Friend friend = world.Engine.Cloud.Friends.FindFriend(f =>
                        f.FriendUsername.Equals(username, StringComparison.InvariantCultureIgnoreCase));

                    if (friend == null)
                    {
                        UniLog.Log(username + " is not in the friends list, cannot auto-invite", false);
                    }
                    else
                    {
                        if (HeadlessTweaks.config.GetValue(HeadlessTweaks.AutoInviteOptOut).Contains(friend.FriendUserId)) continue;

                        MessageManager.UserMessages messages = world.Engine.Cloud.Messages.GetUserMessages(friend.FriendUserId);
                        if (!string.IsNullOrWhiteSpace(info.AutoInviteMessage))
                        {
                            int num1 = await messages.SendTextMessage(info.AutoInviteMessage) ? 1 : 0;
                        }
                        world.AllowUserToJoin(friend.FriendUserId);
                        int num2 = await messages.SendMessage(messages.CreateInviteMessage(world)) ? 1 : 0;
                        UniLog.Log(username + " invited.", false);
                        friend = null;
                        messages = null;
                    }
                }
            });
        }




        private static UserMessages getUserMessages(string userId)
        {
            return Engine.Current.Cloud.Messages.GetUserMessages(userId);
        }

        private static PermissionLevel getUserPermissionLevel(string userId)
        {
            return HeadlessTweaks.config.GetValue(HeadlessTweaks.PermissionLevels).FirstOrDefault(x => x.Key == userId).Value;
        }




        // Commands
        class Commands
        {
            [Command("help", "Shows this help message")]
            public static void help(UserMessages userMessages, Message obj, string[] args)
            {
                string help = "";
                foreach (var method in typeof(Commands).GetMethods())
                {
                    var attr = method.GetCustomAttribute<CommandAttribute>();
                    if (attr != null)
                    {
                        // skip if permission level is higher than the user
                        if (getUserPermissionLevel(obj.SenderId) < attr.PermissionLevel) continue;
                        
                        help += attr.Name + " - " + attr.Description + "\n";
                    }
                }
                userMessages.SendTextMessage(help);
            }
            // Invite me to a specific world by name or to the current world if no name is given
            // Usage: /reqInvite [world name]

            [Command("reqInvite", "Requests an invite to a world")]
            public static void reqinvite(UserMessages userMessages, Message obj, string[] args)
            {
                if (args.Length < 1)
                {
                    userMessages.SendInviteMessage(Engine.Current.WorldManager.FocusedWorld.GetSessionInfo());
                    return;
                }
                string worldName = string.Join(" ", args);
                worldName.Trim();
                var worlds = Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld);

                World world = worlds.Where(w => w.RawName == worldName || w.SessionId == worldName).FirstOrDefault();
                if (world == null)
                {
                    if (int.TryParse(worldName, out var result))
                    {
                        var worldList = worlds.ToList();
                        if (result < 0 || result >= worldList.Count)
                        {
                            userMessages.SendTextMessage("World index out of range");
                            return;
                        }
                        world = worldList[result];
                    } else {
                        userMessages.SendTextMessage("No world found with the name " + worldName);
                        return;
                    }
                }
                userMessages.SendInviteMessage(world.GetSessionInfo());
            }

            // Toggle Opt out of auto-invites
            // Usage: /optOut

            [Command("optOut", "Toggles opt out of auto-invites")]
            public static void optout(UserMessages userMessages, Message obj, string[] args)
            {
                var optOut = HeadlessTweaks.config.GetValue(HeadlessTweaks.AutoInviteOptOut);
                if (optOut.Contains(obj.SenderId)) {
                    optOut.Remove(obj.SenderId);
                    userMessages.SendTextMessage("Opted in to auto-invites");
                } else {
                    optOut.Add(obj.SenderId);
                    userMessages.SendTextMessage("Opted out of auto-invites");
                }
                HeadlessTweaks.config.Set(HeadlessTweaks.AutoInviteOptOut, optOut);
                HeadlessTweaks.config.Save();
            }
            // Set permission level for a user
            // Usage: /setPerm [user id] [level]
            // level is PermissionLevel enum or int value
            // User can not set their own permission level
            // Target permission must be lower than or equal to your own

            [Command("setPerm", "Sets a user's permission level", PermissionLevel.Moderator)]
            public static void setperm(UserMessages userMessages, Message obj, string[] args)
            {
                if (args.Length < 2)
                {
                    userMessages.SendTextMessage("Usage: /setperm [user id] [level]");
                    return;
                }
                string userId = args[0];
                string level = args[1];

                if (userId == obj.SenderId)
                {
                    userMessages.SendTextMessage("You can not set your own permission level");
                    return;
                } else if (!userId.ToLower().StartsWith("u-")) {
                    userMessages.SendTextMessage($"'{userId}' is not a valid user id");
                    return;
                } else if (getUserPermissionLevel(userId) > getUserPermissionLevel(obj.SenderId)) {
                    userMessages.SendTextMessage("You can not set a user's permission level who is higher than you");
                    return;
                }

                // less < greater
                
                if (Enum.TryParse(level, true, out PermissionLevel levelEnum))
                {
                    // check if level is higher than your own
                    if (getUserPermissionLevel(obj.SenderId) < levelEnum)
                    {
                        userMessages.SendTextMessage("You can not set a user's permission level higher than yours");
                        return;
                    }
                    var levels = HeadlessTweaks.config.GetValue(HeadlessTweaks.PermissionLevels);
                    levels[userId] = levelEnum;
                    HeadlessTweaks.config.Set(HeadlessTweaks.PermissionLevels, levels);
                    HeadlessTweaks.config.Save();
                    
                    userMessages.SendTextMessage("Permission level set to " + levelEnum);
                } else {
                    userMessages.SendTextMessage("Invalid permission level");
                }
            }

            // Mark all as read
            // Usage: /markAllRead

            [Command("markAllRead", "Marks all messages as read")]
            public static void markallread(UserMessages userMessages, Message obj, string[] args)
            {
                userMessages.MarkAllRead();
            }

            // Save world
            // if no name is given, it will save the world the user is in
            // Usage: /saveWorld [world name]

            [Command("saveWorld", "Saves a world", PermissionLevel.Moderator)]
            public static async void saveworld(UserMessages userMessages, Message obj, string[] args)
            {
                World world = null;
                if (args.Length < 1)
                {
                    var userWorlds = Engine.Current.WorldManager.Worlds.Where(w => w.GetUserByUserId(obj.SenderId) != null);
                    if (userWorlds.Count() != 0)
                    {
                        world = userWorlds.FirstOrDefault((w) => w.GetUserByUserId(obj.SenderId).IsPresentInWorld);
                    }
                    if (world == null)
                    {  // if no world found tell the user
                        userMessages.SendTextMessage("You are not in a world");
                        return;
                    }

                    await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);
                    userMessages.SendTextMessage("Saved world " + world.Name);
                    return;
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
                            userMessages.SendTextMessage("World index out of range");
                            return;
                        }
                        world = worldList[result];
                    }
                    else
                    {
                        userMessages.SendTextMessage("No world found with the name " + worldName);
                        return;
                    }
                }
                await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);

                userMessages.SendTextMessage("Saved world " + worldName);
            }

            private static World GetWorld(UserMessages userMessages, string worldName)
            {
                var worlds = Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld);
                var world = worlds.Where(w => w.RawName == worldName || w.SessionId == worldName).FirstOrDefault();
                if (world == null)
                {
                    if (int.TryParse(worldName, out var result))
                    {
                        var worldList = worlds.ToList();
                        if (result < 0 || result >= worldList.Count)
                        {
                            userMessages.SendTextMessage("World index out of range");
                            return null;
                        }
                        world = worldList[result];
                    }
                    else
                    {
                        userMessages.SendTextMessage("No world found with the name " + worldName);
                        return null;
                    }
                }
                return world;
            }

            // Get world or user's world
            // helper function for /getWorld and /getUserWorld
            private static World GetWorldOrUserWorld(UserMessages userMessages, string worldName, string userId)
            {
                World world = null;

                if (worldName == null)
                { // if no world name given, get the user's world
                    var userWorlds = Engine.Current.WorldManager.Worlds.Where(w => w.GetUserByUserId(userId) != null);
                    if (userWorlds.Count() == 0)
                    {
                        world = userWorlds.FirstOrDefault((w) => w.GetUserByUserId(userId).IsPresentInWorld);
                    }
                    if (world == null)
                    {  // if no world found tell the user
                        userMessages.SendTextMessage("User is not in a world");
                        return null;
                    }
                    return world;
                }
                world = GetWorld(userMessages, worldName);
                return world;
            }
            // Start a new world from a template
            // Usage: /startWorldTemplate [template name]

            [Command("startWorldTemplate", "Start a new world from a template", PermissionLevel.Moderator)]
            public static async void startworldtemplate(UserMessages userMessages, Message obj, string[] args)
            {
                if (args.Length < 1)
                {
                    userMessages.SendTextMessage("Usage: /startWorldTemplate [template name]");
                    return;
                }
                string templateName = string.Join(" ", args);
                templateName.Trim();

                WorldStartupParameters startInfo = new WorldStartupParameters {
                    LoadWorldPresetName = templateName
                };
                if ((await WorldPresets.GetPresets()).FirstOrDefault(p => 
                    p.Name != null && p.Name.Equals(startInfo.LoadWorldPresetName, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    userMessages.SendTextMessage("Invalid preset name");
                    return;
                }

                var traverse = Traverse.CreateWithType("NeosHeadless.Program");//.TypeExists
               
                if(!traverse.TypeExists())
                {
                    userMessages.SendTextMessage("Error Program Not Found :(");
                    return;
                }
                var handler = traverse.Field<NeosHeadless.CommandHandler>("commandHandler").Value;
                if (handler == null)
                {
                    userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
                }

                var newWorld = new NeosHeadless.WorldHandler(handler.Engine, handler.Config, startInfo);
                await newWorld.Start();
                userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());

            }

            // List worlds
            // Usage: /worlds


            [Command("worlds", "List all worlds")]
            public static async void worlds(UserMessages userMessages, Message obj, string[] args)
            {
                int num = 0;
                foreach (World world1 in Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld))
                {
                    await userMessages.SendTextMessage(string.Format("[{0}] {1}\n Users: {2}\n Present: {3}\n\n ", 
                        num.ToString(), 
                        world1.RawName, 
                        world1.UserCount, 
                        world1.ActiveUserCount) + string.Format("AccessLevel: {0}\n MaxUsers: {1}", world1.AccessLevel, world1.MaxUsers));
                    ++num;
                }
            }

            // Get user permission level
            // Usage: /getPerm [User ID]

            [Command("getPerm", "Get user permission level", PermissionLevel.Moderator)]
            public static void getperm(UserMessages userMessages, Message obj, string[] args)
            {
                var userId = obj.SenderId;
                if (args.Length >= 1)
                {
                    userId = args[0];
                    if (!userId.ToLower().StartsWith("u-"))
                    {
                        userMessages.SendTextMessage($"'{userId}' is not a valid user id");
                        return;
                    }
                }
                userMessages.SendTextMessage($"{userId} has a permission level of {getUserPermissionLevel(userId)}");
            }

            // List users in a world
            // Usage: /usersInWorld [world name]
            // If no world name is given, it will list the users in the world the user is in

            [Command("usersInWorld", "List users in a world", PermissionLevel.Moderator)]



            // Command Attributes
            // Name: The name of the command
            // Description: The description of the command
            // PermissionLevel: The permission level required to use the command

            internal class CommandAttribute : Attribute
            {
                
                
                public string Name { get; set; }
                public string Description { get; set; }
                public PermissionLevel PermissionLevel { get; set; }



                public CommandAttribute(string name, string description, PermissionLevel permissionLevel = PermissionLevel.None)
                {
                    Name = name;
                    Description = description;
                    PermissionLevel = permissionLevel;
                }
            }
        }
    }
}