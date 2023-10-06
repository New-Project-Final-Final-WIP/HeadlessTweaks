using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using SkyFrost.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace HeadlessTweaks
{
    public static class AutoInviteOptOut
    {
        internal static void Init(Harmony harmony)
        {

            var target = typeof(WorldStartSettingsExtensions).GetMethod(nameof(WorldStartSettingsExtensions.SetWorldParameters));
            var prefix = typeof(AutoInviteOptOut).GetMethod(nameof(AutoInvitePrefix));
            var postfix = typeof(AutoInviteOptOut).GetMethod(nameof(AutoInvitePostfix));

            harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
        }


        public static void AutoInvitePrefix(WorldStartupParameters info, out List<string> __state)
        {
            if (info.AutoInviteUsernames == null)
            {
                __state = null;
                return;
            }
            __state = info.AutoInviteUsernames.ToList(); // Create a copy
            info.AutoInviteUsernames.Clear();
        }
        public static async void AutoInvitePostfix(WorldStartupParameters info, List<string> __state, World world, Task __result)
        {
            if (__state == null || __state.Count <= 0) return;

            if (world.Engine.Cloud.CurrentUser == null)
            {
                UniLog.Log("Not logged in, cannot send auto-invites!");
                return;
            }

            /* The hubris of programmers can be a cause of their downfall, especially when dealing with complex systems.
             * This act of laziness can lead to a cascade of issues that can exacerbate the original problem and make it difficult to resolve.
             * Despite this, some programmers settle with a different bad solution that works and is bad, rather than seeking help or taking the time to transpile a method.
             * It takes humility and discipline to do things properly and avoid taking shortcuts that can lead to long-term negative consequences. 
             * That humility is not mine.
            */
            await __result; // :)
            info.AutoInviteUsernames.AddRange(__state);

            foreach (string username in __state)
            {
                Contact contact = world.Engine.Cloud.Contacts.FindContact(f =>
                    f.ContactUsername.Equals(username, StringComparison.InvariantCultureIgnoreCase));

                if (contact == null)
                {
                    UniLog.Log(username + " is not in the contacts list, cannot auto-invite", false);
                    continue;
                }
                if (HeadlessTweaks.AutoInviteOptOutList.GetValue().Contains(contact.ContactUserId)) continue;

                UserMessages messages = world.Engine.Cloud.Messages.GetUserMessages(contact.ContactUserId);

                world.AllowUserToJoin(contact.ContactUserId);
                if (!string.IsNullOrWhiteSpace(info.AutoInviteMessage))
                {
                    await messages.SendTextMessage(info.AutoInviteMessage);
                }
                await messages.SendMessage(await messages.CreateInviteMessage(world));
                UniLog.Log(username + " invited.");
            }
        }
    }
}
