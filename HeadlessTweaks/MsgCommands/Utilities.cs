using System.Linq;
using FrooxEngine;

using static CloudX.Shared.MessageManager;

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

    }
}
