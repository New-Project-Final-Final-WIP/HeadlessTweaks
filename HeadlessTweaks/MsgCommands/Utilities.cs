using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using BaseX;

using static CloudX.Shared.MessageManager;
using CloudX.Shared;
using System;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        private static UserMessages GetUserMessages(string userId)
        {
            return Engine.Current.Cloud.Messages.GetUserMessages(userId);
        }

        private static PermissionLevel GetUserPermissionLevel(string userId)
        {
            return HeadlessTweaks.PermissionLevels.GetValue().FirstOrDefault(x => x.Key == userId).Value;
        }

        private static bool CanUserJoin(World world, string userId)
        {
            return GetUserPermissionLevel(userId) > PermissionLevel.None || world.IsUserAllowed(userId);
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
        private static World GetWorldOrUserWorld(UserMessages userMessages, string worldName, string userId, bool defaultFocused = false)
        {
            World world = null;
            // if world is null or blank space
            if (string.IsNullOrWhiteSpace(worldName))
            { // if no world name given, get the user's world
                var userWorlds = Engine.Current.WorldManager.Worlds.Where(w => w.GetUserByUserId(userId) != null);
                if (userWorlds.Count() == 0)
                {
                    world = userWorlds.FirstOrDefault((w) => w.GetUserByUserId(userId).IsPresentInWorld);
                }
                if (world == null)
                {  // if no world found tell the user
                    if (!defaultFocused)
                    {
                        userMessages.SendTextMessage("User is not in a world");
                        return null;
                    }
                    world = Engine.Current.WorldManager.FocusedWorld;
                }
                return world;
            }
            world = GetWorld(userMessages, worldName);
            return world;
        }


        // Get world url from object uri

        private static async Task<Uri> ExtractOrbUrl(UserMessages userMessages, string objectUri)
        {
            //var tcs = new TaskCompletionSource<Message>();

            Uri url = null;
            var w = Userspace.UserspaceWorld;
            await w.Coroutines.StartTask(async () =>
            {
                Slot slot;
                await new NextUpdate();
                slot = w.RootSlot.AddSlot("SpawnedItem");
                await slot.LoadObjectAsync(new Uri(objectUri));

                var orb = slot.GetComponentInChildren<WorldOrb>();
                if (orb == null)
                {
                    _ = userMessages.SendTextMessage("Not a world orb");
                    return;
                }
                Uri worldUrl = orb.URL;
                if (worldUrl == null)
                {
                    _ = userMessages.SendTextMessage("No world url");
                    return;
                }
                url = worldUrl;
                slot.Destroy();
            });
            return url;
        }



        // A static getter for a HarmonyLib.Traverse instance that caches the Traverse instance on first use and returns the same instance on subsequent uses.  
        // This is useful for getting a Traverse instance that can be used on multiple threads.

        private static NeosHeadless.CommandHandler GetCommandHandler()
        {
            if (_commandHandler == null)
            {
                _commandHandler = Traverse.CreateWithType("NeosHeadless.Program")?
                    .Field<NeosHeadless.CommandHandler>("commandHandler")?
                    .Value;
            }
            return _commandHandler;
        }
        private static NeosHeadless.CommandHandler _commandHandler = null;


        class BatchMessageHelper
        {
            public UserMessages UserMessages { get; private set; }
            // list of messages to send
            public List<string> Messages { get; private set; }

            public string Prefix { get; private set; }

            // constructor
            public BatchMessageHelper(UserMessages userMessages, string prefix = null)
            {
                UserMessages = userMessages;
                Messages = new List<string>();
                Prefix = prefix;
            }

            // add a message to the list
            public void Add(string message)
            {
                if (Messages.Count != 0 && Messages.Last().Length + message.Length < 512)
                {
                    Messages[Messages.Count - 1] += "\n" + message;
                }
                else
                {
                    Messages.Add(Prefix + message);
                }

            }

            // add message colr
            public void Add(string message, color color)
            {
                var newMessage = "<color=" + color.ToHexString(color.a != 1f) + ">" + message + "</color>";
                Add(newMessage);
            }

            // send the messages
            public async void Send()
            {
                foreach (var message in Messages)
                {
                    await UserMessages.SendTextMessage(message);
                }
            }
        }
    }
}