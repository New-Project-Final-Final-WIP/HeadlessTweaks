using System.Collections.Generic;
using System.Linq;
using FrooxEngine;
using CloudX.Shared;
using BaseX;

using static CloudX.Shared.MessageManager;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        partial class Commands
        {
            // Message User
            // Usage: /message [user id] [message]

            [Command("message", "Message a user", PermissionLevel.Owner, usage: "[user id] [message]")]
            public static void Message(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    userMessages.SendTextMessage("Usage: /message [user id] [message]");
                    return;
                }

                string userId = args[0];
                string message = string.Join(" ", args.Skip(1));

                var um = GetUserMessages(userId);

                if (um == null)
                {
                    userMessages.SendTextMessage("User not friends with headless");
                    return;
                }
                um.SendTextMessage(message);
            }

            // List headless friends
            // Usage: /friends

            [Command("friends", "List headless friends", PermissionLevel.Owner)]
            public static void Friends(UserMessages userMessages, Message msg, string[] args)
            {
                // Engine.Current.Cloud.Friends.GetFriends();
                var list = new List<Friend>();
                Engine.Current.Cloud.Friends.GetFriends(list);

                var messages = new BatchMessageHelper(userMessages);
                foreach (var friend in list)
                {
                    if (friend.FriendUserId == "U-Neos")
                        continue;
                    messages.Add($"[{friend.FriendUserId}] {friend.FriendUsername}", friend.FriendStatus == FriendStatus.Requested ? color.Cyan:color.Black, true);
                }
                messages.Send();
            }

            // Accept friend request
            // Usage: /acceptFriend [user id]

            [Command("acceptFriend", "Accept friend request", PermissionLevel.Owner, usage: "[user id]")]
            public static void Acceptfriend(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    userMessages.SendTextMessage("Usage: /acceptFriend [user id]");
                    return;
                }

                string userId = args[0];

                var friend = Engine.Current.Cloud.Friends.GetFriend(userId);

                if (friend == null || friend.FriendStatus != FriendStatus.Requested)
                {
                    userMessages.SendTextMessage("There's no friend request from that user");
                    return;
                }
                Engine.Current.Cloud.Friends.AddFriend(friend);

                userMessages.SendTextMessage("Friend request accepted");
            }

            // Add friend
            // Usage: /addFriend [user id]

            [Command("addFriend", "Add friend", PermissionLevel.Owner, usage: "[user id]")]
            public static void Addfriend(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    userMessages.SendTextMessage("Usage: /acceptFriend [user id]");
                    return;
                }

                string userId = args[0];

                var friend = Engine.Current.Cloud.Friends.GetFriend(userId);

                if (friend != null)
                {
                    if (friend.FriendStatus == FriendStatus.Requested)
                    {
                        Engine.Current.Cloud.Friends.AddFriend(friend);
                        userMessages.SendTextMessage("Friend request accepted");
                        return;
                    }
                    userMessages.SendTextMessage("That user is already friends");
                    return;
                }
                Engine.Current.Cloud.Friends.AddFriend(userId);
                userMessages.SendTextMessage("Friend request sent");
            }

            // Remove friend
            // Usage: /removeFriend [user id]

            [Command("removeFriend", "Remove friend", PermissionLevel.Owner, usage: "[user id]")]
            public static void Removefriend(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    userMessages.SendTextMessage("Usage: /removeFriend [user id]");
                    return;
                }

                string userId = args[0];

                var friend = Engine.Current.Cloud.Friends.GetFriend(userId);

                if (friend == null)
                {
                    userMessages.SendTextMessage("That user is not friends with headless");
                    return;
                }
                Engine.Current.Cloud.Friends.RemoveFriend(friend);
                userMessages.SendTextMessage("Friend removed");
            }
        }
    }
}
