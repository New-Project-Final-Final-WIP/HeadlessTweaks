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

                var messages = new BatchMessageHelper(userMessages);
                messages.Add(world.UserCount + " users in world " + world.Name + ((world.UserCount != 0) ? ":" : ""));
                foreach (var user in users)
                {
                    // Return username
                    // grey out if user is not present in the world 
                    if (user.IsPresent)
                    {
                        messages.Add(user.UserName);
                    }
                    else
                    {
                        messages.Add("<i>" + user.UserName + "</i>", color.Black.SetA(0.75f));
                    }
                }
                messages.Send();
            }

            // Save world
            // if no name is given, it will save the world the user is in
            // Usage: /saveWorld [world name]

            [Command("saveWorld", "Saves a world", PermissionLevel.Moderator)]
            public static async void SaveWorld(UserMessages userMessages, Message msg, string[] args)
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
            public static async void StartWorldTemplate(UserMessages userMessages, Message msg, string[] args)
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

                var handler = GetCommandHandler();
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
            public static async void StartWorldUrl(UserMessages userMessages, Message msg, string[] args)
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
                var handler = GetCommandHandler();
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


            // Start a world from a world orb
            // Usage: /startWorldOrb [SessionAccessLevel]

            [Command("startWorldOrb", "Start a world from a world orb", PermissionLevel.Moderator)]
            public static async void StartWorldOrb(UserMessages userMessages, Message msg, string[] args)
            {
                string accessLevel = string.Join(" ", args);
                accessLevel.Trim();

                // Contacts to friends
                if (accessLevel.ToLower() == "contacts")
                {
                    accessLevel = "Friends";
                }
                if (accessLevel.ToLower() == "contacts+")
                {
                    accessLevel = "FriendsOfFriends";
                }


                // SessionAccessLevel is an emum
                // parse the string to an enum value
                SessionAccessLevel? sessionAccessLevel = SessionAccessLevel.Private;
                if (Enum.TryParse(accessLevel, true, out SessionAccessLevel parsedAccessLevel))
                {
                    sessionAccessLevel = parsedAccessLevel;
                }
                else if (args.Length > 0)
                {
                    _ = userMessages.SendTextMessage("Invalid access level, Defaulting to Private");
                }

                // ask the user to send a world orb
                _ = userMessages.SendTextMessage("Send a world orb");

                var response = await userMessages.WaitForResponse(45);

                if (response == null)
                    return;

                // Check if response type is an item
                if (response.MessageType != CloudX.Shared.MessageType.Object)
                {
                    _ = userMessages.SendTextMessage("Invalid response");
                    return;
                }
                // Extract the record from the message
                FrooxEngine.Record record = response.ExtractContent<FrooxEngine.Record>();
                var itemUri = record.AssetURI;

                Slot slot;
                // Check if the record is a world orb
                var w = Userspace.UserspaceWorld;
                _ = userMessages.SendTextMessage("Checking if object is a world orb, Spawning orb...");
                await w.Coroutines.StartTask(async () =>
                { // RunSynchronously did not want to work
                    await new NextUpdate();
                    slot = w.RootSlot.AddSlot("SpawnedItem");
                    await slot.LoadObjectAsync(new Uri(itemUri));

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

                    slot.Destroy();
                    _ = userMessages.SendTextMessage("Found world orb, starting world...");
                    // Create the world
                    WorldStartupParameters startInfo = new WorldStartupParameters
                    {
                        LoadWorldURL = worldUrl.ToString(),
                        AccessLevel = sessionAccessLevel.Value
                    };

                    var handler = GetCommandHandler();
                    if (handler == null)
                    {
                        _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                        return;
                    }

                    _ = userMessages.SendTextMessage("Starting world...");
                    var newWorld = new NeosHeadless.WorldHandler(handler.Engine, handler.Config, startInfo);
                    await newWorld.Start();

                    // Check if world is started
                    if (newWorld.CurrentInstance == null)
                    {
                        _ = userMessages.SendTextMessage("Error starting world");
                        return;
                    }


                    newWorld.CurrentInstance.AllowUserToJoin(msg.SenderId);
                    _ = userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());
                });
            }

            // Set role for a user in a world
            // Target sender user's focused world if no world name is given
            // Usage: /role [user] [role name] [world name]

            [Command("role", "Set role for a user in a world", PermissionLevel.Administrator)]
            public static async void Role(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    _ = userMessages.SendTextMessage("Usage: /role [user] [role name] [world name]");
                    return;
                }
                string userId = args[0];
                string roleName = args[1];

                string worldName = "";
                if (args.Length > 2)
                {
                    worldName = string.Join(" ", args.Skip(2));
                };

                // Get the world
                var world = GetWorldOrUserWorld(userMessages, worldName, userId, true);
                if (world == null)
                    return;

                var user = world.GetUserByUserId(userId);
                if (user == null)
                {
                    _ = userMessages.SendTextMessage("User not found");
                    return;
                }

                var role = world.Permissions.Roles.FirstOrDefault(x => x.RoleName.Value.Equals(roleName, StringComparison.InvariantCultureIgnoreCase));

                if (role == null)
                {
                    _ = userMessages.SendTextMessage($"Role {roleName} not found");
                    return;
                }
                else if (role > world.HostUser.Role)
                {
                    _ = userMessages.SendTextMessage("Cannot assign role higher than host user");
                    return;
                }
                world.RunSynchronously(() =>
                { // Datamodel moment :((())))))))
                    user.Role = role;
                    world.Permissions.AssignDefaultRole(user, role);

                    _ = userMessages.SendTextMessage($"Set {user.UserName} to {role.RoleName}");
                });
            }

            // Set own role
            // Usage: /roleSelf [role name] [world name]

            [Command("roleSelf", "Set own role", PermissionLevel.Administrator)]
            public static async void RoleSelf(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /roleSelf [role name] [world name]");
                    return;
                }

                // Add user id at the beginning of the args
                var newArgs = args.ToList();
                newArgs.Insert(0, msg.SenderId);
                
                // Call the set role command
                Role(userMessages, msg, newArgs.ToArray());
            }
        }
    }
}
