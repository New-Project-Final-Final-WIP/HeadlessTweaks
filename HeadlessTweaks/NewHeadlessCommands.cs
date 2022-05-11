using HarmonyLib;
using System;
using NeosHeadless;
namespace HeadlessTweaks
{
    class NewHeadlessCommands
    {
        internal static void Init(Harmony harmony)
        {
            HeadlessTweaks.Msg("NewHeadlessCommands.Init");
            var yeah = typeof(NeosCommands).GetMethod("SetupCommonCommands");
            var what = typeof(NewCommands).GetMethod("Postfix");

            harmony.Patch(yeah, postfix: new HarmonyMethod(what));
        }

        [HarmonyPatch(typeof(NeosCommands), "SetupCommonCommands")]
        class NewCommands
        {
            public static void Postfix(CommandHandler handler)
            {
                // Set user permission command
                HeadlessTweaks.Msg("Setting up user permission command");
                handler.RegisterCommand(new GenericCommand("setUserPermission", "Sets a user's permission level", "<user> <permission>", (h, world, args) =>
                {
                    if (args.Count != 2)
                    {
                        HeadlessTweaks.Warn("Please include a user and a permission level");
                        return;
                    }

                    var user = args[0];
                    var permission = args[1];

                    if (Enum.TryParse(permission, true, out PermissionLevel levelEnum))
                    {
                        var levels = HeadlessTweaks.config.GetValue(HeadlessTweaks.PermissionLevels);
                        levels[user] = levelEnum;
                        HeadlessTweaks.config.Set(HeadlessTweaks.PermissionLevels, levels);
                        HeadlessTweaks.config.Save();

                        HeadlessTweaks.Msg("Permission level set to " + levelEnum);
                    }
                    else
                    {
                        HeadlessTweaks.Warn("Invalid permission level");
                    }
                }));
                
                if (HeadlessTweaks.config.GetValue(HeadlessTweaks.UseDiscordWebhook))
                {
                    handler.RegisterCommand(new GenericCommand("sendToDiscord", "Sends a message to discord", "<message>", (h, world, args) =>
                    {
                        if (args.Count == 0)
                        {
                            HeadlessTweaks.Warn("Please include a message");
                            return;
                        }

                        DiscordIntegration.DiscordHelper.sendEmbed(string.Join(" ", args.ToArray()), new Discord.Color(0xbb5ec8));
                    }));
                }
            }
        }
    }
}
