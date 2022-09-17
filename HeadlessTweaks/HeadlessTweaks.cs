using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using CloudX.Shared;

namespace HeadlessTweaks
{
    public class HeadlessTweaks : NeosMod
    {
        public override string Name => "HeadlessTweaks";
        public override string Author => "New-Project-Final-Final-WIP";
        public override string Version => "1.3.0";
        public override string Link => "https://github.com/New-Project-Final-Final-WIP/HeadlessTweaks";

        public static bool isHeadless;

        public static ModConfiguration config;

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> UseDiscordWebhook = new ModConfigurationKey<bool>("UseDiscordWebhook", "Use Discord Webhook", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookID = new ModConfigurationKey<string>("DiscordWebhookID", "Discord Webhook ID", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookKey = new ModConfigurationKey<string>("DiscordWebhookKey", "Discord Webhook Key", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookUsername = new ModConfigurationKey<string>("DiscordWebhookUsername", "Discord Webhook Username", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookAvatar = new ModConfigurationKey<string>("DiscordWebhookAvatar", "Discord Webhook Avatar", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<List<string>> DiscordWebhookDisabledEvents = new ModConfigurationKey<List<string>>("DiscordWebhookDisabledEvents", "Discord Webhook Events without notification", () => new List<string>());
        
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<List<string>> AutoInviteOptOut = new ModConfigurationKey<List<string>>("AutoInviteOptOut", "Auto Invite Opt Out", () => new List<string>(), internalAccessOnly: true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, PermissionLevel>> PermissionLevels = new ModConfigurationKey<Dictionary<string, PermissionLevel>>("PermissionLevels", "Permission Levels", () => new Dictionary<string, PermissionLevel>());

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, string>> WorldRoster = new ModConfigurationKey<Dictionary<string, string>>("WorldRoster", "World Roster", () => new Dictionary<string, string>());

        // Rename sessions in webhook
        // SessionIds to Name
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, string>> SessionIdToName = new ModConfigurationKey<Dictionary<string, string>>("SessionIdToName", "SessionIdToName", () => new Dictionary<string, string>() {
            
        });


        // Default session access level for new sessions
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<SessionAccessLevel> DefaultSessionAccessLevel = new ModConfigurationKey<SessionAccessLevel>("DefaultSessionAccessLevel", "Default Session Access Level", () => SessionAccessLevel.FriendsOfFriends);

        // Default session hidden status for new sessions
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> DefaultSessionHidden = new ModConfigurationKey<bool>("DefaultSessionHidden", "Default Session Hidden", () => true);

        // secondary alpha for lists in messages float
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<float?> AlternateListAlpha = new ModConfigurationKey<float?>("AlternateListAlpha", "Alternate List Alpha", () => 0.7f);



        // Disable autosave if no one is in world
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> SmartAutosaveEnabled = new ModConfigurationKey<bool>("SmartAutosave", "Disable autosave if there are no users in current world", () => false);




        // ConfigSaved 
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> configSaved = new ModConfigurationKey<bool>("_configSaved", "_configSaved", internalAccessOnly:true);


        


        public override void OnEngineInit()
        {
            config = GetConfiguration();
            // Initialize default values
            if (!config.TryGetValue(configSaved, out bool saved) || !saved)
            {
                config.Set(configSaved, true);
                config.Save(true);
            }


            Harmony harmony = new Harmony("me.New-Project-Final-Final-WIP.HeadlessTweaks");
            
            // Check if we are loaded by a headless client
            Type neosHeadless = AccessTools.TypeByName("NeosHeadless.Program");
            isHeadless = neosHeadless != null;

            // Check if the Discord namespace exists
            // If it does, we can assume that the Discord.NET library is installed
            // and we can init the Discord client

            bool discordExists = AccessTools.TypeByName("Discord.Webhook.DiscordWebhookClient") != null;
            Msg(discordExists ? "Discord.NET library found" : "Discord.NET library not found");
            
            if (config.GetValue(UseDiscordWebhook) && discordExists) DiscordIntegration.Init(harmony);
            if (config.GetValue(UseDiscordWebhook) && !discordExists) 
                Warn("Discord.NET library not found, but the UseDiscordWebhook option is enabled. Please put the Discord.NET library dlls in nml_libs to use this feature.");
            
            // If we are not loaded by a headless client skip the rest
            if (!isHeadless)
            {
                Warn("Headless Not Detected! Skipping headless specific modules");
                return;
            }
            NewHeadlessCommands.Init(harmony);
            MessageCommands.Init(harmony);
            SmartAutosave.Init(harmony);
        }
    }
}