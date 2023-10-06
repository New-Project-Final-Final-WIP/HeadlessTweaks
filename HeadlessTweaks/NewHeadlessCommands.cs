using HarmonyLib;
using System;
using FrooxEngine.Headless;
using FrooxEngine;

namespace HeadlessTweaks
{
    class NewHeadlessCommands
    {
        internal static void Init(Harmony harmony)
        {
            var target = typeof(HeadlessCommands).GetMethod(nameof(HeadlessCommands.SetupCommonCommands));
            var postfix = typeof(NewHeadlessCommands).GetMethod(nameof(SetupNewCommands));

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        public static void SetupNewCommands(CommandHandler handler)
        {
            handler.RegisterCommand(new GenericCommand("setUserPermission", "Sets a user's permission level", "<user> <permission>", async (h, world, args) =>
            {
                if (args.Count != 2)
                {
                    HeadlessTweaks.Warn("Please include a user and a permission level");
                    return;
                }

                var user = args[0];
                var permission = args[1];

                var userId = await MessageCommands.TryGetUserId(user);

                if (userId == null)
                {
                    HeadlessTweaks.Msg($"Could not find user '{user}', use a user id to override this check");
                    return;
                }


                if (Enum.TryParse(permission, true, out PermissionLevel levelEnum))
                {
                    var levels = HeadlessTweaks.PermissionLevels.GetValue();
                    levels.Add(userId, levelEnum);
                    HeadlessTweaks.PermissionLevels.SetValueAndSave(levels);

                    HeadlessTweaks.Msg($"Permission level set to {levelEnum} for {userId}");
                }
                else
                {
                    HeadlessTweaks.Warn("Invalid permission level");
                }
            }));


            if (HeadlessTweaks.config.GetValue(HeadlessTweaks.UseDiscordWebhook) && HeadlessTweaks.isDiscordLoaded)
            {
                handler.RegisterCommand(new GenericCommand("sendToDiscord", "Sends a message to discord", "<message>", (h, world, args) =>
                {
                    if (args.Count == 0)
                    {
                        HeadlessTweaks.Warn("Please include a message");
                        return;
                    }

                    DiscordIntegration.DiscordHelper.SendEmbed(string.Join(" ", args.ToArray()), RadiantUI_Constants.Hero.PURPLE);
                }));
            }
        }

    }
}
