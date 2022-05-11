using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using CloudX.Shared;
using BaseX;

using static NeosModLoader.NeosMod;

namespace HeadlessTweaks
{
    internal partial class MessageCommands
    {
        // Dictionary of command names and their methods
        private static readonly Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();

        internal static void Init(Harmony harmony)
        {
            // Fetch all the methods that are marked with the Command attribute in the Commands class
            // Store them in a dictionay with the lowercase command name as the key

            // Get all methods under Commands that have the CommandAttribute
            var cmdMethods = typeof(Commands).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetCustomAttributes<CommandAttribute>().Any());

            // Loop through all the methods and add them to the dictionary
            foreach (var method in cmdMethods)
            {
                var cmdName = method.GetCustomAttribute<CommandAttribute>().Name.ToLower();
                commands.Add(cmdName, method);
            }

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

        private static void OnMessageReceived(Message msg)
        {
            Task.Run(async () =>
            {
                DateTime time = DateTime.UtcNow;
                var ids = new List<string> { msg.Id };

                await Engine.Current.Cloud.HubClient.MarkMessagesRead(new MarkReadBatch()
                {
                    SenderId = Engine.Current.Cloud.Messages.SendReadNotification ? msg.SenderId : null,
                    Ids = ids,
                    ReadTime = time
                }).ConfigureAwait(false);
            });
            switch (msg.MessageType)
            {
                case CloudX.Shared.MessageType.Text:
                    if (msg.Content.StartsWith("/"))
                    {
                        var userMessages = GetUserMessages(msg.SenderId);
                        var args = msg.Content.Split(' ');
                        var cmd = args[0].Substring(1).ToLower();
                        var cmdArgs = args.Skip(1).ToArray();


                        var cmdMethod = commands.ContainsKey(cmd) ? commands[cmd] : null;


                        // Check if user has permission to use command
                        // CommandAttribute.PermissionLevel
                        var cmdAttr = cmdMethod?.GetCustomAttribute<CommandAttribute>();
                        if (cmdAttr == null) return;

                        if (cmdAttr.PermissionLevel > GetUserPermissionLevel(msg.SenderId))
                        {
                            userMessages.SendTextMessage("You do not have permission to use that command.");
                            return;
                        }

                        if (cmdMethod == null)
                        {
                            userMessages.SendTextMessage("Unknown command");
                            return;
                        }
                        Msg("Executing command: " + cmd);
                        // Try to execute command and send error message if it fails
                        try
                        {
                            cmdMethod.Invoke(null, new object[] { userMessages, msg, cmdArgs });
                        }
                        catch (Exception e)
                        {
                            // HeadlessTweaks.Error failed to execute user's command and send error message
                            Error($"Failed to execute command from {msg.SenderId}: " + cmd, e);
                            userMessages.SendTextMessage("Error: " + e.Message);
                        }
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
                        if (HeadlessTweaks.AutoInviteOptOut.GetValue().Contains(friend.FriendUserId)) continue;

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
    }
}