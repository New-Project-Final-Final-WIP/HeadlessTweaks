using System;
using FrooxEngine;
using System.Linq;
using SkyFrost.Base;

using System.Threading.Tasks;
using Elements.Quantity;

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
                messages.Add($"{world.UserCount} users in the world \"{world.Name}\"{((world.UserCount != 0)?':':null)}");
                foreach (var user in users)
                {
                    string usrMsg = user.UserName;
                    if (!user.IsPresent)
                    {
                        messages.Add($"<i>{usrMsg}</i>", RadiantUI_Constants.Neutrals.MIDLIGHT);
                        continue;
                    }
                    messages.Add(usrMsg);
                }
                _ = messages.Send();
            }

            // Save world
            // if no name is given, it will save the world the user is in
            // Usage: /saveWorld [?world name...]

            [Command("saveWorld", "Saves a world", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
            public static async Task SaveWorld(UserMessages userMessages, Message msg, string[] args)
            {
                string worldName = string.Join(" ", args).Trim();

                World world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId);
                if (world == null) return;

                _ = userMessages.SendTextMessage($"Starting save for world \"{world.Name}\"");
                var currentTime = DateTime.Now;
                await Userspace.SaveWorldAuto(world, SaveType.Overwrite, false);

                // QuantityX has nice to string formatting for a lot of things, though this looks a little silly
                Time timeElapsed = new((DateTime.Now - currentTime).TotalSeconds);
                _ = userMessages.SendTextMessage($"Saved world \"{world.Name}\" in {timeElapsed.FormatAuto("0.##")}");
            }

            // Start a new world from a template
            // Usage: /startWorldTemplate [template name] [?SessionAccessLevel]

            [Command("startWorldTemplate", "Start a new world from a template", "World Management", PermissionLevel.Moderator, usage: "[template name] [?SessionAccessLevel]", "startTemplateWorld")]
            public static async Task StartWorldTemplate(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /startWorldTemplate [template name] [?SessionAccessLevel]");
                    return;
                }

                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();
                if (args.Length > 1 && !TryParseAccessLevel(args[1], out sessionAccessLevel))
                {
                    _ = userMessages.SendTextMessage($"Invalid access level \"{args[1]}\"");
                    return;
                }

                WorldStartupParameters startInfo = new() {
                    LoadWorldPresetName = args[0],
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };
                if (WorldPresets.Presets.FirstOrDefault(p => p.Name != null && p.Name.Equals(startInfo.LoadWorldPresetName, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    _ = userMessages.SendTextMessage($"Unrecognized preset name \"{startInfo.LoadWorldPresetName}\"");
                    return;
                }
                await CommandWorldSetup(userMessages, startInfo);
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

                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();
                if (args.Length > 1 && !TryParseAccessLevel(args[1], out sessionAccessLevel))
                {
                    _ = userMessages.SendTextMessage($"Invalid access level \"{args[1]}\"");
                    return;
                }

                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                if (!WorldRoster.TryGetValue(args[0], out string worldUrl)) return;

                WorldStartupParameters startInfo = new() {
                    LoadWorldURL = worldUrl,
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };

                await CommandWorldSetup(userMessages, startInfo);
            }

            // List world templates
            // Usage: /worldTemplates

            [Command("worldTemplates", "List world templates", "World Management", PermissionLevel.Moderator)]
            public static void WorldTemplates(UserMessages userMessages, Message msg, string[] args)
            {
                var messages = new BatchMessageHelper(userMessages);
                messages.Add("World templates:");

                foreach (var template in WorldPresets.Presets) 
                    messages.Add(template.Name, true);


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

                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();
                if (args.Length > 1 && !TryParseAccessLevel(args[1], out sessionAccessLevel))
                {
                    _ = userMessages.SendTextMessage($"Invalid access level \"{args[1]}\"");
                    return;
                }

                WorldStartupParameters startInfo = new()
                {
                    LoadWorldURL = args[0],
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };
                await CommandWorldSetup(userMessages, startInfo);
            }

            // Start a world from a world orb
            // Usage: /startWorldOrb [?SessionAccessLevel]
            [Command("startWorldOrb", "Start a world from a world orb", "World Management", PermissionLevel.Moderator, usage: "[?SessionAccessLevel]")]
            public static async Task StartWorldOrb(UserMessages userMessages, Message msg, string[] args)
            {
                // SessionAccessLevel is an emum
                // parse the string to an enum value
                SessionAccessLevel sessionAccessLevel = HeadlessTweaks.DefaultSessionAccessLevel.GetValue();
                if (args.Length > 1 && !TryParseAccessLevel(args[0], out sessionAccessLevel))
                {
                    _ = userMessages.SendTextMessage($"Invalid access level \"{args[0]}\"");
                    return;
                }

                // ask the user to send a world orb

                var record = await userMessages.RequestObjectMessage("Send a world orb");
                if (record == null) return;

                _ = userMessages.SendTextMessage("Checking if object is a world orb...");

                var worldUrl = await ExtractOrbUrl(userMessages, record.AssetURI);
                if (worldUrl == null)
                    return;

                // Create the world
                WorldStartupParameters startInfo = new()
                {
                    LoadWorldURL = worldUrl.ToString(),
                    AccessLevel = sessionAccessLevel,
                    HideFromPublicListing = HeadlessTweaks.DefaultSessionHidden.GetValue()
                };

                await CommandWorldSetup(userMessages, startInfo);
            }

            // Set role for a user in a world
            // Target sender user's focused world if no world name is given
            // Usage: /role [user] [role name] [?world name...]
            [Command("role", "Set role for a user in a world", "World Management", PermissionLevel.Administrator, usage: "[user] [role name] [?world name...]")]
            public static async Task Role(UserMessages userMessages, Message msg, string[] args)
            {
                if (args.Length < 2)
                {
                    _ = userMessages.SendTextMessage("Usage: /role [user] [role name] [?world name...]");
                    return;
                }

                string userArg = args[0];
                string userId = await TryGetUserId(userArg);

                if (userId == null)
                {
                    _ = userMessages.SendTextMessage($"User \"{userArg}\" not found");
                    return;
                }

                string worldName = null;
                if (args.Length > 2)
                {
                    worldName = string.Join(" ", args.Skip(2));
                };

                // Get the world
                var world = GetWorldOrUserWorld(userMessages, worldName, userId, true);
                if (world == null) return;

                var user = world.GetUserByUserId(userId);
                if (user == null)
                {
                    _ = userMessages.SendTextMessage($"User \"{userArg}\" not found in world \"{world.Name}\"");
                    return;
                }

                string roleName = args[1];
                var role = world.Permissions.Roles.FirstOrDefault(x => x.RoleName.Value.Equals(roleName, StringComparison.InvariantCultureIgnoreCase));

                if (role == null)
                {
                    _ = userMessages.SendTextMessage($"Role \"{roleName}\" not found");
                    return;
                }
                else if (role > world.HostUser.Role)
                {
                    _ = userMessages.SendTextMessage("Cannot assign role higher than host user");
                    return;
                }

                // Datamodel moment :((())))))))
                await world.Coroutines.StartTask(async () => {
                    await new ToWorld();
                    user.Role = role;
                    world.Permissions.AssignDefaultRole(user, role);
                });
                _ = userMessages.SendTextMessage($"Set user \"{user.UserName}\" to \"{role.RoleName}\"");
            }

            // Set own role
            // Usage: /roleSelf [role name] [?world name...]
            [Command("roleSelf", "Set own role", "World Management", PermissionLevel.Administrator, usage: "[role name] [?world name...]")]
            public static async Task RoleSelf(UserMessages userMessages, Message msg, string[] args)
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
                await Role(userMessages, msg, newArgs.ToArray());
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
                string worldUrl = null;
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

                if (string.IsNullOrWhiteSpace(worldUrl))
                {
                    // Ask the user to send a world orb
                    var recievedObject = await userMessages.RequestObjectMessage("No Uri provided, send a world orb");

                    if (recievedObject == null) return;

                    // Check if the object is a world orb
                    var worldUrlResult = await ExtractOrbUrl(userMessages, recievedObject.AssetURI);

                    if (worldUrlResult == null) return;

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

                _ = userMessages.SendTextMessage($"Added \"{worldName}\" to the world roster");
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

                string worldName = string.Join(" ", args);

                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();
                // Check if world name is unique
                if (!WorldRoster.ContainsKey(worldName))
                {
                    _ = userMessages.SendTextMessage($"World \"{worldName}\" doesn't exist in the roster");
                    return;
                }

                // Remove the world from the world roster
                WorldRoster.Remove(worldName);

                // Save the world roster
                HeadlessTweaks.WorldRoster.SetValueAndSave(WorldRoster);

                _ = userMessages.SendTextMessage($"Removed \"{worldName}\" from the world roster");

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

                foreach (var world in worlds)
                    messages.Add(world, true);
                
                _ = messages.Send();
            }

            // List all worlds both in the world roster list and in the template list
            // Usage: /listWorlds
            [Command("listWorlds", "List all worlds in the world roster and in the world presets", "World Management", PermissionLevel.Moderator)]
            public static void ListWorlds(UserMessages userMessages, Message msg, string[] args)
            {
                var WorldRoster = HeadlessTweaks.WorldRoster.GetValue();

                var worlds = WorldRoster.Keys.ToList();
                worlds.Sort();

                var messages = new BatchMessageHelper(userMessages);

                messages.Add("<b>World Roster:</b>");
                foreach (var world in worlds)
                    messages.Add(world, true);

                messages.Add("<b>World Templates:</b>");
                foreach (var world in WorldPresets.Presets)
                    messages.Add(world.Name, true);
                
                _ = messages.Send();
            }

            // Close world command
            // Usage: /closeWorld [?world name...]

            [Command("closeWorld", "Close a world", "World Management", PermissionLevel.Moderator, usage: "[?world name...]")]
            public static async Task CloseWorld(UserMessages userMessages, Message msg, string[] args)
            {
                /*if (args.Length < 1)
                {
                    _ = userMessages.SendTextMessage("Usage: /closeWorld [?world name...]");
                    return;
                }*/
                // Get the world name from joining the args

                var worldName = string.Join(" ", args);

                // Get the users world or focused world
                var world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId, true);
                if (world == null)
                    return;

                // Wait for response from the user to confirm they want to close the world
                var confirmMsg = await userMessages.RequestTextMessage($"Are you sure you want to close the world \"{world.Name}\"? (y/n)", 32);
                if (confirmMsg == null)
                {
                    _ = userMessages.SendTextMessage($"Not closing the world \"{world.Name}\"");
                    return;
                }
                var confirmText = confirmMsg.ToLower();

                if (confirmText == "y" || confirmText == "yes")
                {
                    // Close the world
                    world.Destroy();
                    _ = userMessages.SendTextMessage($"World \"{world.Name}\" closed");
                }
                else
                {
                    _ = userMessages.SendTextMessage($"Canceled, not closing world \"{world.Name}\"");
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
                    _ = userMessages.SendTextMessage($"Invalid access level \"{args[0]}\"");
                    return;
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

                _ = userMessages.SendTextMessage($"Session access level set to {(hidden ? "Hidden, " : null)}{accessLevel} for \"{world.Name}\"");
            }

            // set session name command
            // Usage: /setSessionName [?target world...]
            // alias: setWorldName, worldName, sessionName
            [Command("setSessionName", "Set the name of a session", "World Management", PermissionLevel.Moderator, usage: "[?target world...]", "setWorldName", "worldName", "sessionName")]
            public static async Task SetSessionName(UserMessages userMessages, Message msg, string[] args)
            {
                // Get the world name from joining the args
                var worldName = string.Join(" ", args).Trim();

                // Get the users world or focused world
                var world = GetWorldOrUserWorld(userMessages, worldName, msg.SenderId, false);
                if (world == null) return;

                // Get the new name
                var newName = await userMessages.RequestTextMessage("Enter the new name for the session");
                if (newName == null) return;

                // Set the name
                world.Name = newName;

                _ = userMessages.SendTextMessage($"Session name set to \"{newName}\"");
            }
        }
    }
}
