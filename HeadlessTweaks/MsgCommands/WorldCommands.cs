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

                // Error starting world 
                if (newWorld.CurrentInstance == null)
                {
                    _ = userMessages.SendTextMessage("Error starting world");
                    return;
                }

                newWorld.CurrentInstance.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());

            }

            // Start a world from the world roster
            // Usage: /startWorld [world name]

            [Command("startWorld", "Start a world", PermissionLevel.Moderator)]
            public static async void StartWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorld [world name]");
                    return;
                }
                string worldName = args[0];

                
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                // Get world from world roster
                // Key is the world name, value is the world url
                if (!WorldRoster.ContainsKey(worldName))
                    return;

                // Get world url
                string worldUrl = WorldRoster[worldName];


                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldURL = worldUrl
                };
                
                // Get world handler
                var handler = GetCommandHandler();

                // Message user that the world is starting
                _ = userMessages.SendTextMessage("Starting world " + worldName);

                // Start world
                var newWorld = new NeosHeadless.WorldHandler(handler.Engine, handler.Config, startInfo);
                await newWorld.Start();

                // Error starting world 
                if (newWorld.CurrentInstance == null)
                {
                    _ = userMessages.SendTextMessage("Error starting world " + worldName);
                    return;
                }

                newWorld.CurrentInstance.AllowUserToJoin(msg.SenderId);
                _ = userMessages.SendInviteMessage(newWorld.CurrentInstance.GetSessionInfo());
            }

            // List world templates
            // Usage: /worldTemplates

            [Command("worldTemplates", "List world templates", PermissionLevel.Moderator)]
            public static async void WorldTemplates(UserMessages userMessages, Message msg, string[] args)
            {
                var templates = await WorldPresets.GetPresets();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("World templates:");
                foreach (var template in templates)
                {
                    messages.Add(template.Name);
                }
                messages.Send();
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

                // Error starting world 
                if (newWorld.CurrentInstance == null)
                {
                    _ = userMessages.SendTextMessage("Error starting world");
                    return;
                }

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

                var record = await userMessages.RequestObjectMessage("Send a world orb");

                var itemUri = record.AssetURI;

                _ = userMessages.SendTextMessage("Checking if object is a world orb, Spawning orb...");

                var worldUrl = await ExtractOrbUrl(userMessages, itemUri);
                if (worldUrl == null)
                    return;

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
            }

            // Set role for a user in a world
            // Target sender user's focused world if no world name is given
            // Usage: /role [user] [role name] [world name]
            [Command("role", "Set role for a user in a world", PermissionLevel.Administrator)]
            public static void Role(UserMessages userMessages, Message msg, string[] args)
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
            public static void RoleSelf(UserMessages userMessages, Message msg, string[] args)
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

            // Add a world to the world roster list
            // World Name can't have spaces and must be unique
            // World url is optional
            // If no world url is given, ask the user to send a world orb
            // Usage: /addWorld [world name] [world url]
            [Command("addWorld", "Add a world to the world roster list", PermissionLevel.Moderator)]
            public static async void AddWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /addWorld [world name] [world url]");
                    return;
                }

                string worldName = args[0];
                string worldUrl = "";
                if (args.Length > 1)
                {
                    worldUrl = args[1];
                }

                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                // Check if world name is unique
                if (WorldRoster.ContainsKey(worldName))
                {
                    _ = userMessages.SendTextMessage($"{worldName} already exists");
                    return;
                }

                if (worldUrl == "")
                {
                    // Ask the user to send a world orb
                    var record = await userMessages.RequestObjectMessage("No uri provided, send a world orb");
                    
                    // Check if the object is a world orb
                    var worldUrlResult = await ExtractOrbUrl(userMessages, record.AssetURI);
                    if (worldUrlResult == null)
                        return;
                    worldUrl = worldUrlResult.ToString();
                }

                // check if the world url is valid
                if (!Uri.IsWellFormedUriString(worldUrl, UriKind.Absolute))
                {
                    _ = userMessages.SendTextMessage("Invalid world url");
                    return;
                }

                // Add the world to the world roster
                WorldRoster.Add(worldName, worldUrl);

                // Save the world roster
                HeadlessTweaks.WorldRoster.SetValueAndSave(WorldRoster);

                _ = userMessages.SendTextMessage($"Added {worldName} to the world roster");
            }

            // Remove a world from the world roster list
            // Usage: /removeWorld [world name]
            [Command("removeWorld", "Remove a world from the world roster list", PermissionLevel.Moderator)]
            public static void RemoveWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /removeWorld [world name]");
                    return;
                }

                string worldName = args[0];

                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                // Check if world name is unique
                if (!WorldRoster.ContainsKey(worldName))
                {
                    _ = userMessages.SendTextMessage($"{worldName} doesn't exist");
                    return;
                }

                // Remove the world from the world roster
                WorldRoster.Remove(worldName);

                // Save the world roster
                HeadlessTweaks.WorldRoster.SetValueAndSave(WorldRoster);

                _ = userMessages.SendTextMessage($"Removed {worldName} from the world roster");

            }

            // List all worlds in the world roster list
            // Usage: /listWorlds
            [Command("listRosterWorlds", "List all worlds in the world roster list", PermissionLevel.Moderator)]
            public static void ListRosterWorlds(UserMessages userMessages, Message msg, string[] args)
            {
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();

                var worlds = WorldRoster.Keys.ToList();
                worlds.Sort();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("World Roster:");
                foreach (var world in worlds)
                {
                    messages.Add(world);
                }
                messages.Send();
            }

            // List all worlds both in the world roster list and in the template list
            // Usage: /listWorlds
            [Command("listWorlds", "List all worlds in the world roster and in the world presets", PermissionLevel.Moderator)]
            public static async void ListWorlds(UserMessages userMessages, Message msg, string[] args)
            {
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                var WorldTemplates = await WorldPresets.GetPresets();

                var worlds = WorldRoster.Keys.ToList();
                worlds.Sort();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("<b>World Roster:</b>");
                foreach (var world in worlds)
                {
                    messages.Add(world);
                }

                messages.Add("<b>World Templates:</b>");
                foreach (var world in WorldTemplates)
                {
                    messages.Add(world.Name);
                }
                messages.Send();
            }
        }
    }
}
