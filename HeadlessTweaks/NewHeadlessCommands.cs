using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeosHeadless;
using FrooxEngine;
namespace HeadlessTweaks
{
    class NewHeadlessCommands
    {
        internal static void Init(Harmony harmony)
        {
            var yeah = typeof(NeosCommands).GetMethod("SetupCommonCommands");
            var what = typeof(NewCommands).GetMethod("Postfix");

            harmony.Patch(yeah, postfix: new HarmonyMethod(what));
        }

        [HarmonyPatch(typeof(NeosCommands), "SetupCommonCommands")]
        class NewCommands
        {
            public static void Postfix(CommandHandler handler)
            {
                if (HeadlessTweaks.config.UseDiscordWebhook)
                {
                    handler.RegisterCommand((ICommand)new GenericCommand("sendToDiscord", "Sends a message to discord", "<message>", (AsyncCommandAction)(async (h, world, args) =>
                    {
                        if (args.Count == 0)
                        {
                            HeadlessTweaks.Warn("Please include a message");
                            return;
                        }

                        DiscordIntegration.DiscordHelper.sendEmbed(String.Join(" ", args.ToArray()), new Discord.Color(0xbb5ec8));
                    })));
                }
            }
        }
    }
}
