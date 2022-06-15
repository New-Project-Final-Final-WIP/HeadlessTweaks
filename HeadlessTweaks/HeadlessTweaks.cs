using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;

namespace HeadlessTweaks
{
    public class HeadlessTweaks : NeosMod
    {
        public override string Name => "HeadlessTweaks";
        public override string Author => "New-Project-Final-Final-WIP";
        public override string Version => "1.0.0";
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
        public static readonly ModConfigurationKey<string> DiscordWebhookUsername = new ModConfigurationKey<string>("DiscordWebhookUsername", "Discord Webhook Username", () => "New Headless");  // TODO: Change to "Headless"
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookAvatar = new ModConfigurationKey<string>("DiscordWebhookAvatar", "Discord Webhook Avatar", () => "https://newweb.page/assets/images/logo.png"); // TODO: change to some other image
        
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
            { "S-U-New-Headless:New_Scene", "New Scene" },
            { "S-U-Birdfather:What", "Who"}
        });




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
        }
    }
}