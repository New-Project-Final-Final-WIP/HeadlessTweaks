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
                var messages = new BatchMessageHelper(userMessages);

                // This is messy becaus I am rushing, I'll clean this up eventually
                List<Contact>[] categories = new List<Contact>[4];
                categories[0] = new List<Contact>();
                categories[1] = new List<Contact>();
                categories[2] = new List<Contact>();
                categories[3] = new List<Contact>();

                string[] groups = new string[]
                {
                    "Requested Contacts: ",
                    "Accepted Contacts: ",
                    "Ignored Requests: ",
                    "Blocked Contacts: ",
                };

                /*
                    Requested, // Show purple
		            Ignored, // Hide
		            Blocked, // Show red append (BLOCKED)?
		            Accepted // Show normally
                */

                Engine.Current.Cloud.Contacts.ForeachContact(contact =>
                {
                    if (contact.ContactUserId == userMessages.Cloud.Platform.AppUserId || contact.IsSelfContact)
                        return;
                    
                    colorX color = RadiantUI_Constants.TEXT_COLOR;

                    if (contact.IsContactRequest)
                    {
                        categories[0].Add(contact);
                    } else if (contact.ContactStatus == ContactStatus.Accepted)
                    {
                        categories[1].Add(contact);
                    } else if (contact.ContactStatus == ContactStatus.Ignored)
                    {
                        categories[2].Add(contact);
                    } else if (contact.ContactStatus == ContactStatus.Blocked)
                    {
                        categories[3].Add(contact);
                    }
                });

                for (int i = 0; i < categories.Length; i++)
                {
                    if (categories[i].Count == 0) continue;

                    messages.Add($"<size=130%>{groups[i]}</size>", false, true);
                    
                    for(int j = 0; j < categories[i].Count; j++)
                    {
                        messages.Add($"{categories[i][j].ContactUsername} - [{categories[i][j].ContactUserId}]", true);
                    }
                }

                _ = messages.Send();
            }

            // Add contact
            // Usage: /addContact [user]

            [Command("addContact", "Add contact", "Headless Management", PermissionLevel.Owner, usage: "[user]")]
            public static async Task Addcontact(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /addContact [user]");
                    return;
                }

                string userId = await TryGetUserId(args[0]);
                
                var contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact != null)
                {
                    if (contact.ContactStatus == ContactStatus.Accepted)
                    {
                        _ = userMessages.SendTextMessage($"User \"{args[0]}\" is already contacts with this headless");
                        return;
                    } else if (contact.ContactStatus == ContactStatus.Blocked)
                    {
                        _ = userMessages.SendTextMessage($"User \"{args[0]}\" is blocked from this headless");
                        return;
                    }

                    await Engine.Current.Cloud.Contacts.AddContact(contact);
                    _ = userMessages.SendTextMessage($"Contact request accepted from user \"{args[0]}\"");
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

                if(userId == null)
                {
                    _ = userMessages.SendTextMessage($"User \"{args[0]}\" could not be found");
                    return;
                }

                var contact = Engine.Current.Cloud.Contacts.GetContact(userId);

                if (contact != null && Engine.Current.Cloud.Contacts.IsContact(userId))
                {
                    await Engine.Current.Cloud.Contacts.RemoveContact(contact);

                    _ = userMessages.SendTextMessage($"Removed \"{args[0]}\" from contacts");
                    return;
                }


                if (contact != null && contact.IsContactRequest)
                {
                    await Engine.Current.Cloud.Contacts.IgnoreRequest(contact);

                    _ = userMessages.SendTextMessage($"Ingoring contact request from \"{args[0]}\"");
                    return;
                }

                _ = userMessages.SendTextMessage($"User \"{args[0]}\" is not contacts with this headless");
            }
        }
    }
}
