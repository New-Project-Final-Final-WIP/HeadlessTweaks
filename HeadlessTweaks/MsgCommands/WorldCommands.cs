using BaseX;
using System;
using FrooxEngine;
using System.Linq;
using CloudX.Shared;

using static CloudX.Shared.MessageManager;
using System.Threading.Tasks;

namespace HeadlessTweaks
{
    partial class MessageCommands
    {
        public partial class Commands
        {
            // List users in a world
            // Usage: /usersInWorld [?world name...]
            // If no world name is given, it will list the users in the world the user is in

            [Command("users", "List users in a world", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
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
            // Usage: /saveWorld [?world name...]

            [Command("saveWorld", "Saves a world", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
            public static async Task SaveWorld(UserMessages userMessages, Message msg, string[] args)
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
            // Usage: /startWorldTemplate [template name...] [?SessionAccessLevel]

            [Command("startWorldTemplate", "Start a new world from a template", "World Management", PermissionLevel.Moderator, usage: "[template name...] [?SessionAccessLevel]", "startTemplateWorld")]
            public static async Task StartWorldTemplate(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorldTemplate [template name...] [?SessionAccessLevel]");
                    return;
                }

                // Check if last arg is a SessionAccessLevel
                string sessionAccessLevelString = args[args.Length - 1];

                bool isSessionAccessLevel = false;
                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();

                if (args.Length > 1)
                {
                    isSessionAccessLevel = TryParseAccessLevel(sessionAccessLevelString, out SessionAccessLevel parsedAccessLevel);
                    if (isSessionAccessLevel)
                    {
                        sessionAccessLevel = parsedAccessLevel;
                    }
                }

                // Get template by name
                string templateName = string.Join(" ", args.Take(args.Length - (isSessionAccessLevel ? 1 : 0)));
                templateName.Trim();

                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldPresetName = templateName,
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };
                if ((await WorldPresets.GetPresets()).FirstOrDefault(p =>
                    p.Name != null && p.Name.Equals(startInfo.LoadWorldPresetName, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    _ = userMessages.SendTextMessage($"Invalid preset name '{startInfo.LoadWorldPresetName}'");
                    return;
                }

                var handler = GetCommandHandler();
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
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
                        _ = userMessages.SendTextMessage("No name entered, using default name");
                    }
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
            // Usage: /startWorld [world name] [?SessionAccessLevel]

            [Command("startWorld", "Start a world", "World Management", PermissionLevel.Moderator, usage: "[world name] [?SessionAccessLevel]")]
            public static async Task StartWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorld [world name] [?SessionAccessLevel]");
                    return;
                }
                string worldName = args[0];

                
                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();

                if (args.Length > 1)
                {
                    if (TryParseAccessLevel(args[1], out SessionAccessLevel parsedAccessLevel))
                    {
                        sessionAccessLevel = parsedAccessLevel;
                    }
                    else
                    {
                        _ = userMessages.SendTextMessage("Invalid access level, Defaulting to Private");
                    }
                }

                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                // Get world from world roster
                // Key is the world name, value is the world url
                if (!WorldRoster.ContainsKey(worldName))
                    return;

                // Get world url
                string worldUrl = WorldRoster[worldName];


                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldURL = worldUrl,
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };


                // Get world handler
                var handler = GetCommandHandler();
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
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
                        _ = userMessages.SendTextMessage("No name entered, using default name");
                    }
                }

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

            [Command("worldTemplates", "List world templates", "World Management", PermissionLevel.Moderator)]
            public static async Task WorldTemplates(UserMessages userMessages, Message msg, string[] args)
            {
                var templates = await WorldPresets.GetPresets();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("World templates:");
                foreach (var template in templates)
                {
                    messages.Add(template.Name, true);
                }
                _ = messages.Send();
            }

            // Start a world from a url
            // Usage: /startWorldUrl [record url] [?SessionAccessLevel]
            [Command("startWorldUrl", "Start a world from a url", "World Management", PermissionLevel.Moderator, usage: "[record url] [?SessionAccessLevel]")]
            public static async Task StartWorldUrl(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorldUrl [record url] [?SessionAccessLevel]");
                    return;
                }
                string url = args[0];

                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();

                if (args.Length > 1)
                {
                    if (TryParseAccessLevel(args[1], out SessionAccessLevel parsedAccessLevel))
                    {
                        sessionAccessLevel = parsedAccessLevel;
                    }
                    else
                    {
                        _ = userMessages.SendTextMessage("Invalid access level, Defaulting to Private");
                    }
                }

                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldURL = url,
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };
                var handler = GetCommandHandler();
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
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
                        _ = userMessages.SendTextMessage("No name entered, using default name");
                    }
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
            // Usage: /startWorldOrb [?SessionAccessLevel]
            [Command("startWorldOrb", "Start a world from a world orb", "World Management", PermissionLevel.Moderator, usage: "[?SessionAccessLevel]")]
            public static async Task StartWorldOrb(UserMessages userMessages, Message msg, string[] args)
            {
                // SessionAccessLevel is an emum
                // parse the string to an enum value
                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();

                if (args.Length > 0)
                {
                    if (TryParseAccessLevel(args[0], out SessionAccessLevel parsedAccessLevel))
                    {
                        sessionAccessLevel = parsedAccessLevel;
                    }
                    else
                    {
                        _ = userMessages.SendTextMessage("Invalid access level, Defaulting to Private");
                    }
                }

                // ask the user to send a world orb

                var record = await userMessages.RequestObjectMessage("Send a world orb");
                if (record == null)
                {
                    _ = userMessages.SendTextMessage("No world orb sent");
                    return;
                }
                var itemUri = record.AssetURI;

                _ = userMessages.SendTextMessage("Checking if object is a world orb, Spawning orb...");

                var worldUrl = await ExtractOrbUrl(userMessages, itemUri);
                if (worldUrl == null)
                    return;

                _ = userMessages.SendTextMessage("Found world orb");
                // Create the world
                WorldStartupParameters startInfo = new WorldStartupParameters
                {
                    LoadWorldURL = worldUrl.ToString(),
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };

                var handler = GetCommandHandler();
                if (handler == null)
                {
                    _ = userMessages.SendTextMessage("Error Handler Not Found :(");
                    return;
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
                        _ = userMessages.SendTextMessage("No name entered, using default name");
                    }
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
            // Usage: /role [user] [role name] [?world name...]
            [Command("role", "Set role for a user in a world", "World Management", PermissionLevel.Administrator, usage: "[user] [role name] [?world name...]")]
            public static void Role(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    _ = userMessages.SendTextMessage("Usage: /role [user] [role name] [?world name...]");
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
            // Usage: /roleSelf [role name] [?world name...]
            [Command("roleSelf", "Set own role", "World Management", PermissionLevel.Administrator, usage: "[role name] [?world name...]")]
            public static void RoleSelf(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /roleSelf [role name] [?world name...]");
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
            // Usage: /addWorld [world name] [?world url]
            [Command("addWorld", "Add a world to the world roster list", "World Management", PermissionLevel.Moderator, usage: "[world name] [?world url]")]
            public static async Task AddWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /addWorld [world name] [?world url]");
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
            // Usage: /removeWorld [?world name...]
            [Command("removeWorld", "Remove a world from the world roster list", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
            public static void RemoveWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /removeWorld [?world name...]");
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
            [Command("listRosterWorlds", "List all worlds in the world roster list", "World Management", PermissionLevel.Moderator)]
            public static void ListRosterWorlds(UserMessages userMessages, Message msg, string[] args)
            {
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();

                var worlds = WorldRoster.Keys.ToList();
                worlds.Sort();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("World Roster:");

                // iterate over all worlds and modulate the alpha channel of the color to make them more visible
                foreach (var world in worlds)
                {
                    messages.Add(world, true);
                }

                messages.Send();
            }

            // List all worlds both in the world roster list and in the template list
            // Usage: /listWorlds
            [Command("listWorlds", "List all worlds in the world roster and in the world presets", "World Management", PermissionLevel.Moderator)]
            public static async Task ListWorlds(UserMessages userMessages, Message msg, string[] args)
            {
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                var WorldTemplates = await WorldPresets.GetPresets();

                var worlds = WorldRoster.Keys.ToList();
                worlds.Sort();

                var messages = new BatchMessageHelper(userMessages);
                messages.Add("<b>World Roster:</b>");
                foreach (var world in worlds)
                {
                    messages.Add(world, true);
                }

                messages.Add("<b>World Templates:</b>");
                foreach (var world in WorldTemplates)
                {
                    messages.Add(world.Name, true);
                }
                messages.Send();
            }

            // Close world command
            // Usage: /closeWorld [?world name...]

            [Command("closeWorld", "Close a world", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
            public static async Task CloseWorld(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /closeWorld [?world name...]");
                    return;
                }
                // Get the world name from joining the args

                var worldName = string.Join(" ", args);

                // Get the users world or focused world
                var world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId, true);
                if (world == null)
                    return;

                // Wait for response from the user to confirm they want to close the world
                var confirmMsg = await userMessages.RequestTextMessage("Are you sure you want to close this world? (y/n)", 32);
                if (confirmMsg == null)
                {
                    return;
                }
                var confirmText = confirmMsg.ToLower();

                if (confirmText == "y" || confirmText == "yes")
                {
                    // Close the world
                    world.Destroy();
                    _ = userMessages.SendTextMessage("World closed");
                }
                else
                {
                    _ = userMessages.SendTextMessage("World not closed");
                }
            }

            // set session access level command
            // Usage: /setSessionAccessLevel [SessionAccessLevel] [?hidden] [?world name...]
            [Command("setSessionAccessLevel", "Set the access level of a session", "World Management", PermissionLevel.Moderator, usage: "[access level] [?hidden] [?world name...]")]
            public static void SetSessionAccessLevel(UserMessages userMessages, Message msg, string[] args)
            { // TODO: test this command
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /setSessionAccessLevel [access level] [?hidden] [?world name...]");
                    return;
                }

                if (!TryParseAccessLevel(args[0], out SessionAccessLevel accessLevel))
                {
                    _ = userMessages.SendTextMessage("Invalid access level");
                }

                // Get the hidden flag
                var hidden = true;
                var hiddenParsed = false;
                if (args.Length > 1)
                {
                    hiddenParsed = bool.TryParse(args[1], out hidden);
                }

                // Get the world name from joining the args
                // skip the hidden flag if it was parsed
                var worldName = string.Join(" ", args.Skip(hiddenParsed ? 2 : 1));

                // Get the users world or focused world
                var world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId, false);
                if (world == null)
                    return;

                // Get the session
                world.AccessLevel = accessLevel;
                if (hiddenParsed)
                    world.HideFromListing = hidden;

                _ = userMessages.SendTextMessage($"Session access level set to {(hidden ? "Hidden, " : "")}{accessLevel} for {world.Name}");
            }

            // set session name command
            // Usage: /setSessionName [?target world...]
            // alias: setWorldName, worldName, sessionName
            [Command("setSessionName", "Set the name of a session", "World Management", PermissionLevel.Moderator, usage: "[?target world...]", "setWorldName", "worldName", "sessionName")]
            public static async Task SetSessionName(UserMessages userMessages, Message msg, string[] args)
            {
                // Get the world name from joining the args
                var worldName = string.Join(" ", args);
                worldName.Trim();

                // Get the users world or focused world
                var world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId, false);
                if (world == null)
                    return;

                // Get the new name
                var newName = await userMessages.RequestTextMessage("Enter the new name for the session");
                if (newName == null)
                    return;

                // Set the name
                world.Name = newName;

                _ = userMessages.SendTextMessage($"Session name set to {newName}");
            }
        }
    }
}
