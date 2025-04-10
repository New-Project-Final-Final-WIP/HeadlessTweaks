using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using Elements.Core;


using SkyFrost.Base;
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

        public async static Task<string> TryGetUserId(string value, bool allowAnyUserId = true, bool onlyLookupContacts = false) {
            if (allowAnyUserId && IdUtil.GetOwnerType(value.ToUpper()) == OwnerType.User) return value;

            var contact = Engine.Current.Cloud.Contacts.FindContact((Contact f) => f.ContactUsername.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            
            if (contact != null)
                return contact.ContactUserId;

            if (onlyLookupContacts) return null;

            var user = await Engine.Current.Cloud.Users.GetUserByName(value);
            if (user.IsOK)
                return user.Entity.Id;

            return null;
        }

        public static bool TryParseAccessLevel(string value, out SessionAccessLevel parsedLevel)
        {
            // friends to contacts
            switch (value.Trim().ToLower())
            {
                case "friends":
                    value = "Contacts";
                    break;
                case "contacts+":
                case "friends+":
                case "friendsplus":
                case "friendsoffriends":
                case "contactsofcontacts":
                    value = "ContactsPlus";
                    break;
            }

            return Enum.TryParse(value, true, out parsedLevel);
        }
        

        private static bool CanUserJoin(World world, string userId, bool checkIsAllowed = true)
        {
            bool isContact = Engine.Current.Cloud.Contacts.IsContact(userId);
            bool isContactsOrAbove = world.AccessLevel >= SessionAccessLevel.Contacts;
            return GetUserPermissionLevel(userId) > PermissionLevel.None || (checkIsAllowed && world.IsUserAllowed(userId)) || (isContact && isContactsOrAbove);
        }

        private static World GetWorld(UserMessages userMessages, string worldName)
        {
            var worlds = Engine.Current.WorldManager.Worlds.Where(w => w != Userspace.UserspaceWorld);
            World world = null;

            if (int.TryParse(worldName, out var result))
            {
                var worldList = worlds.ToList();
                if (result < 0 || result >= worldList.Count)
                {
                    userMessages.SendTextMessage("World index out of range");
                    return null;
                }
                world = worldList[result];
            } else
            {
                world = worlds.Where(w => w.RawName == worldName || w.SessionId == worldName).FirstOrDefault();
            }

            if (world == null)
            {
                userMessages.SendTextMessage("No world found with the name " + worldName);
                return null;
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
                if (userWorlds.Any())
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


        private static async Task<World> CommandWorldSetup(UserMessages userMessages, WorldStartupParameters startInfo)
        {

            var handler = GetCommandHandler();
            if (handler == null)
            {
                _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                return null;
            }

            var confirmMsg = await userMessages.RequestTextMessage("Do you want to name this session? (y/n)", 32);
            var confirmText = confirmMsg.ToLower();

            if (confirmText == "y" || confirmText == "yes")
            {
                var nameRequest = await userMessages.RequestTextMessage("Send name:", 45);

                if (nameRequest != null)
                {
                    startInfo.SessionName = nameRequest;
                }
                else
                {
                    _ = userMessages.SendTextMessage("No was name entered, using the world's default name");
                }
            }

            string sessionName = startInfo.SessionName;

            if (string.IsNullOrEmpty(sessionName))
            {
                if (startInfo.LoadWorldURL != null)
                {
                    sessionName = startInfo.LoadWorldURL;
                }
                else if (startInfo.LoadWorldPresetName != null)
                {
                    sessionName = startInfo.LoadWorldPresetName;
                }
                else
                {
                    sessionName = "Unknown Name";
                }
            }


            _ = userMessages.SendTextMessage($"Starting world \"{sessionName}\"");

            var newWorld = new FrooxEngine.Headless.WorldHandler(handler.Engine, handler.Config, startInfo);
            try
            {
                await newWorld.Start();
            }
            catch (Exception ex)
            {
                HeadlessTweaks.Error($"Error starting world {sessionName}:\n\t{ex}");
            }

            // Error starting world 
            if (newWorld.CurrentInstance == null)
            {
                _ = userMessages.SendTextMessage($"Problem starting world \"{sessionName}\"");
                return null;
            }

            //newWorld.CurrentInstance.AllowUserToJoin(userMessages.UserId);
            var inviteMessage = await userMessages.CreateInviteMessage(newWorld.CurrentInstance);
            _ = userMessages.SendMessage(inviteMessage);
            return newWorld.CurrentInstance;
        }


        // Get world url from object uri

        private static async Task<Uri> ExtractOrbUrl(UserMessages userMessages, string objectUri)
        {
            var world = Userspace.UserspaceWorld;

            Uri url = null;
            await world.Coroutines.StartTask(async () =>
            {
                await new NextUpdate();
                Slot slot = world.RootSlot.AddLocalSlot("SpawnedItem");
                await slot.LoadObjectAsync(new Uri(objectUri));
                slot = slot.UnpackInventoryItem(true);

                var orb = slot.GetComponentInChildren<WorldOrb>();
                if (orb == null || orb.URL == null)
                {
                    string error = "Sent orb does not have a world url";

                    if (orb == null) error = $"Object \"{slot.Name}\" is not a world orb";

                    _ = userMessages.SendTextMessage(error);
                    slot.Destroy();
                    return;
                }

                url = orb.URL;
                string name = orb.WorldName ?? slot.Name;
                _ = userMessages.SendTextMessage($"Found world \"{name}\" from sent orb");
                slot.Destroy();
            });

            return url;
        }


        private static FrooxEngine.Headless.CommandHandler GetCommandHandler()
        {
            _commandHandler ??= Traverse.CreateWithType("FrooxEngine.Headless.Program")?
                    .Field<FrooxEngine.Headless.CommandHandler>("commandHandler")?.Value;
            return _commandHandler;
        }
        private static FrooxEngine.Headless.CommandHandler _commandHandler = null;

        // Command delegate type for the command handler
        //(UserMessages userMessages, Message msg, string[] args)
        public delegate void MessageCommandAction(UserMessages userMessages, Message msg, string[] args);

        class BatchMessageHelper(UserMessages userMessages, string prefix = null)
        {
            public UserMessages UserMessages { get; private set; } = userMessages;
            // list of messages to send
            public List<string> Messages { get; private set; } = [];

            public string Prefix { get; private set; } = prefix;

            bool _alphaMod = false;

            // add a message to the list
            public void Add(string message, bool modulateAlpha = false, bool forceNewMessage = false)
            {
                if (modulateAlpha)
                {
                    var secondaryAlpha = HeadlessTweaks.AlternateListAlpha.GetValue();
                    if (_alphaMod && secondaryAlpha.HasValue)
                    {
                        message = AlphaMessage(message, secondaryAlpha.Value);
                    }
                    _alphaMod = !_alphaMod;
                } else {
                    _alphaMod = true; // reset alpha mod to true so if modulation continues it will start with alpha
                }
                // TODO if the message is too long, split it into multiple messages before continuing

                if (!forceNewMessage && Messages.Count != 0 && Messages.Last().Length + message.Length < 512)
                {
                    Messages[^1] += "\n" + message;
                    return;
                }
                Messages.Add(Prefix + message);
            }

            // add message color
            public void Add(string message, colorX color, bool modulateAlpha = false)
            {
                if(color != RadiantUI_Constants.TEXT_COLOR)
                    message = string.Format("<color={0}>{1}</color>", color.ToHexString(color.a != 1f), message);
                Add(message, modulateAlpha);
            }
            
            // add message alpha
            public void Add(string message, float alpha)
            {
                Add(AlphaMessage(message, alpha));
            }

            public string AlphaMessage(string message, float alpha)
            {
                byte a = (byte)MathX.Clamp((int)(alpha * 255f), 0, 256);

                return string.Format("<alpha=#{0}>{1}</alpha>", a.ToString("X2"), message);
            }
            // send the messages
            public async Task Send()
            {
                foreach (var message in Messages)
                    await UserMessages.SendTextMessage(message);
            }
        }
    }
}