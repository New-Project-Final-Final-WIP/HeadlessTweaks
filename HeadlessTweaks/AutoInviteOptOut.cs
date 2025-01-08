using SkyFrost.Base;
using FrooxEngine;
using HarmonyLib;
using System;

namespace HeadlessTweaks
{
    public static class AutoInviteOptOut
    {
        internal static void Init(Harmony harmony)
        {

            var target = typeof(WorldStartSettingsExtensions).GetMethod(nameof(WorldStartSettingsExtensions.SetWorldParameters));
            var prefix = typeof(AutoInviteOptOut).GetMethod(nameof(AutoInvitePrefix));

            harmony.Patch(target, prefix: new HarmonyMethod(method: prefix));
        }


        public static void AutoInvitePrefix(ref WorldStartupParameters info)
        {
            info.AutoInviteUsernames?.RemoveAll(CheckSkippedUsernames);
        }

        private static bool CheckSkippedUsernames(string username)
        {
            // This would be simpler if HeadlessTweaks used usernames in config instead of userids
            var contact = Engine.Current.Cloud.Contacts.FindContact((Contact f) => f.ContactUsername.Equals(username, StringComparison.InvariantCultureIgnoreCase));
            
            if (contact != null)
            {
                return HeadlessTweaks.AutoInviteOptOutList.GetValue().Contains(contact.ContactUserId);
            }

            return false;
        }
    }
}
