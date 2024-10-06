using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using SkyFrost.Base;
using Elements.Core;

using static ResoniteModLoader.ResoniteMod;

namespace HeadlessTweaks
{
    public partial class MessageCommands
    {
        // Dictionary of command names and their methods
        private static readonly Dictionary<string, MethodInfo> _RegisteredCommands = [];

        // Dictionary of UserMessages and TaskCompletionSource of Message
        public static readonly Dictionary<UserMessages, TaskCompletionSource<Message>> responseTasks = [];

        internal static void Init()
        {
            // Fetch all the methods that are marked with the Command attribute in the Commands class
            // Store them in a dictionay with the lowercase command name as the key

            // Get all methods under Commands that have the CommandAttribute
            var cmdMethods = typeof(Commands).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetCustomAttributes<CommandAttribute>().Any());

            // Loop through all the methods and add them to the dictionary
            foreach (var method in cmdMethods)
            {
                var cmdName = method.GetCustomAttribute<CommandAttribute>().Name.ToLower();
                
                _RegisteredCommands.Add(cmdName, method);

                // Add all the aliases to the dictionary
                foreach (var alias in method.GetCustomAttribute<CommandAttribute>().Aliases)
                {
                    _RegisteredCommands.Add(alias.ToLower(), method);
                }
            }

            Engine.Current.RunPostInit(HookIntoMessages);
        }

        private static void HookIntoMessages()
        {
            Engine.Current.Cloud.Messages.OnMessageReceived += OnMessageReceived;
        }

        private static async void OnMessageReceived(Message msg)
        {
            if (Engine.Current.Cloud.HubClient == null) return;

            // Mark message as read
            try
            {
                await Engine.Current.Cloud.HubClient.MarkMessagesRead(new MarkReadBatch()
                {
                    SenderId = Engine.Current.Cloud.Messages.SendReadNotification ? msg.SenderId : null,
                    Ids = [msg.Id],
                    ReadTime = DateTime.UtcNow
                });
            } catch (Exception ex)
            {
                // Ran into an error at one point, keeping this here to log more information if it shows again
                Error($"Marking a message has cause an error!\n\tMessage: {msg}, Content: {msg.Content}\n\tException: {ex}");
                return;
            }

            var userMessages = GetUserMessages(msg.SenderId);
            // check if userMessages is in the response tasks dictionary
            // if it is, set the message and remove it from the dictionary
            // if it isn't, do nothing
            if (responseTasks.TryGetValue(userMessages, out TaskCompletionSource<Message> responseTask))
            {
                responseTasks.Remove(userMessages); // Remove before setting the result to allow multiple response requests to be handled for the same message
                responseTask.TrySetResult(msg);
                return;
            }
            switch (msg.MessageType)
            {
                case SkyFrost.Base.MessageType.Text:
                    if (msg.Content.StartsWith('/'))
                    {
                        var args = StringHelper.ParseArguments(msg.Content);
                        var cmd = args[0].Substring(1).ToLower();
                        var cmdArgs = args.Skip(1).ToArray();

                        _RegisteredCommands.TryGetValue(cmd, out MethodInfo cmdMethod);
                        
                        // Check if user has permission to use command
                        // CommandAttribute.PermissionLevel
                        var cmdAttr = cmdMethod?.GetCustomAttribute<CommandAttribute>();

                        if (cmdMethod == null || cmdAttr == null)
                        {
                            _ = userMessages.SendTextMessage("Unknown command");
                            return;
                        }
                        if (cmdAttr.PermissionLevel > GetUserPermissionLevel(msg.SenderId))
                        {
                            _ = userMessages.SendTextMessage("You do not have permission to use that command.");
                            return;
                        }

                        Msg("Executing command: " + cmd);
                        // Try to execute command and send error message if it fails
                        
                        try
                        {
                            //cmdMethod.Invoke(null, new object[] { userMessages, msg, cmdArgs });
                            // check if the command is async
                            if (cmdMethod.ReturnType == typeof(Task))
                            { // if it is, execute it asynchronously so that we can catch any exceptions
                              // Also good thing to note, apparently you can't catch exceptions from async void methods, so we have to define these as async Task
                              // https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming#avoid-async-void

                                await (Task)cmdMethod.Invoke(null, [userMessages, msg, cmdArgs]);
                            }
                            else
                            {
                                var cmdDelegate = (MessageCommandAction)Delegate.CreateDelegate(typeof(MessageCommandAction), cmdMethod);
                                cmdDelegate(userMessages, msg, cmdArgs);
                            }
                        }
                        catch (Exception e)
                        {
                            Error($"Failed to execute command from {msg.SenderId}: " + cmd, e);
                            _ = userMessages.SendTextMessage("Error: " + e.Message);
                        }
                    }
                    return;
                default:
                    return;
            }
        }

    }
}