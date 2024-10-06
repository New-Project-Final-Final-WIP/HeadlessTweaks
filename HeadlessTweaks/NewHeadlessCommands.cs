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


            /*
            // Specify the "Type Name, Assembly name"
            var program = Type.GetType("FrooxEngine.Headless.Program, Resonite");

            // The target field is STATIC and NONPUBLIC (anything other than public), so we bitwise OR them together with the | char
            var field = program?.GetField("commandHandler", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            // First arg is the object instance, so be cause this is a static field we feed it null instead
            // And cast it to the target type
            var handler = (CommandHandler)field?.GetValue(null);
            



            HeadlessTweaks.Msg("INIT NEW HEADLESS COMMANDS");
            HeadlessTweaks.Msg(program?.FullName);
            HeadlessTweaks.Msg(field?.Name);

            Engine.Current.RunPostInit(() =>
            {
                var handler = (CommandHandler)field?.GetValue(null); // First arg is the object instance, so be cause this is a static field we feed it null instead
                HeadlessTweaks.Msg(handler?.ToString());
                if(handler != null )
                    SetupNewCommands(handler);
            });*/
        }

        public static void SetupNewCommands(CommandHandler handler)
        {
            HeadlessTweaks.Msg("Registering new console commands");
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
                    levels[userId] = levelEnum;
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

                    DiscordIntegration.DiscordHelper.SendEmbed(string.Join(" ", [.. args]), RadiantUI_Constants.Hero.PURPLE);
                }));
            }
        }

    }
}
