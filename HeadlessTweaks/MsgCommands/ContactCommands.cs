using System.Collections.Generic;
using System.Linq;
using FrooxEngine;
using SkyFrost.Base;
using Elements.Core;

using System.Threading.Tasks;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        public partial class Commands
        {
            // Message User
            // Usage: /message [user] [message]

            [Command("message", "Message a user", "Headless Management", PermissionLevel.Owner, usage: "[user] [message...]")]
            public static async Task Message(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    _ = userMessages.SendTextMessage("Usage: /message [user] [message]");
                    return;
                }

                string userId = await TryGetUserId(args[0], onlyLookupContacts: true);

                string message = string.Join(" ", args.Skip(1));

                var um = GetUserMessages(userId);
                
                if (um == null)
                {
                    _ = userMessages.SendTextMessage($"User \"{args[0]}\" is not contacts with this headless");
                    return;
                }
                _ = um.SendTextMessage(message);
            }

            // List headless contacts
            // Usage: /contacts

            [Command("contacts", "List headless contacts", "Headless Management", PermissionLevel.Owner)]
            public static void Contacts(UserMessages userMessages, Message msg, string[] args)
            {
                var list = new List<Contact>();
                Engine.Current.Cloud.Contacts.GetContacts(list);

                var messages = new BatchMessageHelper(userMessages);
                foreach (var contact in list)
                {
                    if (contact.ContactUserId == userMessages.Cloud.Platform.AppUserId || contact.ContactUserId == userMessages.Cloud.CurrentUserID)
                        continue;

                    colorX color = RadiantUI_Constants.TEXT_COLOR;

                    if (contact.ContactStatus == ContactStatus.Requested)
                        color = RadiantUI_Constants.Hero.CYAN;
                    messages.Add($"[{contact.ContactUserId}] {contact.ContactUsername}", color, true);
                }
                _ = messages.Send();
            }

            // Accept contact request
            // Usage: /acceptContact [user]

            [Command("acceptContact", "Accept contact request", "Headless Management", PermissionLevel.Owner, usage: "[user]")]
            public static async Task Acceptcontact(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /acceptContact [user]");
                    return;
                }

                string userId = await TryGetUserId(args[0], onlyLookupContacts: true);

                var contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact == null || contact.ContactStatus != ContactStatus.Requested)
                {
                    _ = userMessages.SendTextMessage($"There's no contact request from user \"{args[0]}\"");
                    return;
                }
                await Engine.Current.Cloud.Contacts.AddContact(contact);

                _ = userMessages.SendTextMessage($"Contact request accepted from user \"{args[0]}\"");
            }

            // Add contact
            // Usage: /addContact [user]

            [Command("addContact", "Add contact", "Headless Management", PermissionLevel.Owner, usage: "[user]")]
            public static async Task Addcontact(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /acceptContact [user]");
                    return;
                }

                string userId = await TryGetUserId(args[0]);
                
                var contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact != null)
                {
                    if (contact.ContactStatus == ContactStatus.Requested)
                    {
                        await Engine.Current.Cloud.Contacts.AddContact(contact);
                        _ = userMessages.SendTextMessage($"Contact request accepted from user \"{args[0]}\"");
                        return;
                    }
                    _ = userMessages.SendTextMessage($"User \"{args[0]}\" is already contacts with this headless");
                    return;
                }

                var foundUserResult = await Engine.Current.Cloud.Users.GetUser(userId);
                
                if(!foundUserResult.IsOK)
                {
                    _ = userMessages.SendTextMessage($"Could not find user \"{args[0]}\"");
                    return;
                }

                await Engine.Current.Cloud.Contacts.AddContact(foundUserResult.Entity.ToContact());
                _ = userMessages.SendTextMessage($"Contact request sent to user \"{args[0]}\"");
            }

            // Remove contact
            // Usage: /removeContact [user id]

            [Command("removeContact", "Remove contact", "Headless Management", PermissionLevel.Owner, usage: "[user]")]
            public static async Task Removecontact(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /removeContact [user]");
                    return;
                }

                string userId = await TryGetUserId(args[0]);

                var contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact == null)
                {
                    _ = userMessages.SendTextMessage($"User \"{args[0]}\" is not contacts with this headless");
                    return;
                }
                await Engine.Current.Cloud.Contacts.RemoveContact(contact);
                _ = userMessages.SendTextMessage($"Removed \"{args[0]}\" from contacts");
            }
        }
    }
}
