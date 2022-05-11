using BaseX;
using System;
using FrooxEngine;
using System.Linq;
using CloudX.Shared;

using static CloudX.Shared.MessageManager;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        partial class Commands
        {
            // List users in a world
            // Usage: /usersInWorld [world name]
            // If no world name is given, it will list the users in the world the user is in

            [Command("users", "List users in a world", PermissionLevel.Moderator)]
            public static void Users(UserMessages userMessages, Message msg, string[] args)
            {
                // Get world by name or user world
                World world = GetWorldOrUserWorld(userMessages, string.Join(" ", args), msg.SenderId, true);
                if (world == null) return;

                // List users in world
                var users = world.AllUsers.ToList();

                _ = userMessages.SendTextMessage(world.UserCount + " users in world " + world.Name + ((world.UserCount != 0) ? ":" : ""));
                foreach (var user in users)
                {
                    // Return username
                    // grey out if user is not present in the world 
                    if (user.IsPresent)
                    {
                        _ = userMessages.SendTextMessage(user.UserName);
                    }
                    else
                    {
                        userMessages.SendTextMessage("<i>" + user.UserName + "</i>", color.Gray.SetA(0.5f));
                    }
                }
            }

            // Save world
            // if no name is given, it will save the world the user is in
            // Usage: /saveWorld [world name]

            [Command("saveWorld", "Saves a world", PermissionLevel.Moderator)]
            public static async void Saveworld(UserMessages userMessages, Message msg, string[] args)
            {
                string worldName = string.Join(" ", args);
                worldName.Trim();

                // Get world by name or user world

                World world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId);
                if (world == null) return;

                await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);

                _ = userMessages.SendTextMessage("Saved world " + worldName);
            }

            // Start a new world from a template
            // Usage: /startWorldTemplate [template name]

            [Command("startWorldTemplate", "Start a new world from a template", PermissionLevel.Moderator)]
            public static async void Startworldtemplate(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorldTemplate [template name]");
                    return;
                }
                string templateName = string.Join(" ", args);
                templateName.Trim();

                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldPresetName = templateName
                };
                if ((await WorldPresets.GetPresets()).FirstOrDefault(p =>
                    p.Name != null && p.Name.Equals(startInfo.LoadWorldPresetName, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    _ = userMessages.SendTextMessage("Invalid preset name");
                    return;
                }

                var traverse = HarmonyLib.Traverse.CreateWithType("NeosHeadless.Program");//.TypeExists

                if (!traverse.TypeExists())
                {
                    _ = userMessages.SendTextMessage("Error Program Not Found :(");
                    return;
                }
                var handler = traverse.Field<NeosHeadless.CommandHandler>("commandHandler").Value;
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
                }

                var newWorld = new NeosHeadless.WorldHandler(handler.Engine, handler.Config, startInfo);
                await newWorld.Start();

                newWorld.CurrentInstance.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());

            }

            // Start a world from a url
            // Usage: /startWorldUrl [record url]

            [Command("startWorldUrl", "Start a world from a url", PermissionLevel.Moderator)]
            public static async void Startworldurl(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorldUrl [record url]");
                    return;
                }
                string url = string.Join(" ", args);
                url.Trim();

                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldURL = url
                };
                var traverse = HarmonyLib.Traverse.CreateWithType("NeosHeadless.Program");//.TypeExists

                if (!traverse.TypeExists())
                {
                    _ = userMessages.SendTextMessage("Error Program Not Found :(");
                    return;
                }
                var handler = traverse.Field<NeosHeadless.CommandHandler>("commandHandler").Value;
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
                }

                var newWorld = new NeosHeadless.WorldHandler(handler.Engine, handler.Config, startInfo);
                await newWorld.Start();

                newWorld.CurrentInstance.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());
            }
        }
    }
}
