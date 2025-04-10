using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FrooxEngine;
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
            RegisterCommands(typeof(Commands));

            Engine.Current.RunPostInit(HookIntoMessages);
        }

        public static void RegisterCommands(Type type)
        {
            // Fetch all the methods that are marked with the Command attribute in the Commands class
            // Store them in a dictionay with the lowercase command name as the key

            // Get all methods under Commands that have the CommandAttribute
            var cmdMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetCustomAttributes<CommandAttribute>().Any());

            // Loop through all the methods and add them to the dictionary
            foreach (var method in cmdMethods)
            {
                var attribute = method.GetCustomAttribute<CommandAttribute>();
                var cmdName = attribute.Name.ToLower();

                _RegisteredCommands.TryAdd(cmdName, method);

                // Add all the aliases to the dictionary
                foreach (var alias in attribute.Aliases)
                {
                    _RegisteredCommands.TryAdd(alias.ToLower(), method);
                }
            }
        }

        private static void HookIntoMessages()
        {
            Engine.Current.Cloud.Messages.OnMessageReceived += OnMessageReceived;
        }

        private static async void OnMessageReceived(Message message)
        {
            if (Engine.Current.Cloud.HubClient == null) return;

            // Mark message as read
            try
            {
                await Engine.Current.Cloud.HubClient.MarkMessagesRead(new MarkReadBatch()
                {
                    SenderId = Engine.Current.Cloud.Messages.SendReadNotification ? message.SenderId : null,
                    Ids = [message.Id],
                    ReadTime = DateTime.UtcNow
                });
            } catch (Exception ex)
            {
                // Ran into an error at one point, keeping this here to log more information if it shows again
                Error($"Marking a message has cause an error!\n\tMessage: {message}, Content: {message.Content}\n\tException: {ex}");
                return;
            }

            var userMessages = GetUserMessages(message.SenderId);
            // check if userMessages is in the response tasks dictionary
            // if it is, set the message and remove it from the dictionary
            // if it isn't, do nothing
            if (responseTasks.TryGetValue(userMessages, out TaskCompletionSource<Message> responseTask))
            {
                responseTasks.Remove(userMessages); // Remove before setting the result to allow multiple response requests to be handled for the same message
                responseTask.TrySetResult(message);
                return;
            }
            
            switch (message.MessageType)
            {
                case SkyFrost.Base.MessageType.Text:
                    if (message.Content.StartsWith('/'))
                    {
                        var args = StringHelper.ParseArguments(message.Content);
                        var cmd = args[0][1..].ToLower();
                        var cmdArgs = args.Skip(1).ToArray();

                        _RegisteredCommands.TryGetValue(cmd, out MethodInfo cmdMethod);
                        
                        // Check if user has permission to use command
                        // CommandAttribute.PermissionLevel
                        var cmdAttr = cmdMethod?.GetCustomAttribute<CommandAttribute>();

                        if (cmdMethod == null || cmdAttr == null)
                        {
                            _ = userMessages.SendTextMessage("Unknown command");
                            break;
                        }
                        if (cmdAttr.PermissionLevel > GetUserPermissionLevel(message.SenderId))
                        {
                            _ = userMessages.SendTextMessage("You do not have permission to use that command.");
                            break;
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

                                await (Task)cmdMethod.Invoke(null, [userMessages, message, cmdArgs]);
                            }
                            else
                            {
                                var cmdDelegate = (MessageCommandAction)Delegate.CreateDelegate(typeof(MessageCommandAction), cmdMethod);
                                cmdDelegate(userMessages, message, cmdArgs);
                            }
                        }
                        catch (Exception e)
                        {
                            Error($"Failed to execute command from {message.SenderId}: " + cmd, e);
                            _ = userMessages.SendTextMessage("Error: " + e.Message);
                        }
                    }
                    break;
                case SkyFrost.Base.MessageType.InviteRequest:
                    if (!HeadlessTweaks.AutoHandleInviteRequests.GetValue())
                        break;

                    var inviteRequest = message.ExtractContent<InviteRequest>();

                    // Skip if it was forwarded to the headless for a specific world
                    if (inviteRequest.ForSessionId != null)
                        break;

                    // Only continue if the sender is the requester
                    if (message.SenderId != inviteRequest.UserIdToInvite)
                        break;

                    var world = Engine.Current.WorldManager.FocusedWorld;

                    Msg($"Handling invite request from {inviteRequest.UsernameToInvite}");

                    // check if user can join world
                    if (CanUserJoin(world, inviteRequest.UserIdToInvite, true))
                    {
                        // Should probably change to await userMessages.CreateInviteMessage(world);
                        world.AllowUserToJoin(inviteRequest.UserIdToInvite);
                        await userMessages.SendInviteMessage(world.GenerateSessionInfo());
                    } else
                    {
                        Msg($"User is not allowed to join {world.RawName}, forwarding to admins in the world");
                        await userMessages.ForwardInviteRequestToAdmins(inviteRequest, world);
                    }
                    break;
            }
        }
    }
}