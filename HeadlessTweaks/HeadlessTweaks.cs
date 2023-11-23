using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using SkyFrost.Base;
using System.Runtime.InteropServices;
using Elements.Core;
using FrooxEngine.Headless;

namespace HeadlessTweaks
{
    public class HeadlessTweaks : ResoniteMod
    {
        public override string Name => "HeadlessTweaks";
        public override string Author => "New-Project-Final-Final-WIP";
        public override string Version => "2.1.0";
        public override string Link => "https://github.com/New-Project-Final-Final-WIP/HeadlessTweaks";

        public static bool isHeadless = false;
        public static bool isDiscordLoaded = false;

        public static ModConfiguration config;

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> UseDiscordWebhook = new("UseDiscordWebhook", "Use Discord Webhook", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookID = new("DiscordWebhookID", "Discord Webhook ID", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookKey = new("DiscordWebhookKey", "Discord Webhook Key", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookUsername = new("DiscordWebhookUsername", "Discord Webhook Username", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookAvatar = new("DiscordWebhookAvatar", "Discord Webhook Avatar", () => null);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<DiscordIntegration.DiscordEvents, bool>> DiscordWebhookEnabledEvents = new("DiscordWebhookEnabledEvents", "Enabled Discord webhook events", () => new()
        {
            { DiscordIntegration.DiscordEvents.EngineStart, false },
        });

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<DiscordIntegration.DiscordEvents, colorX>> DiscordWebhookEventColors = new("DiscordWebhookEventColors", "Discord webhook event colors", () => new());



        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<List<string>> AutoInviteOptOutList = new("AutoInviteOptOut", "Auto Invite Opt Out", () => new List<string>(), internalAccessOnly: true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, PermissionLevel>> PermissionLevels = new("PermissionLevels", "Permission Levels", () => new Dictionary<string, PermissionLevel>());

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, string>> WorldRoster = new("WorldRoster", "World Roster", () => new Dictionary<string, string>());

        // Rename sessions in webhook
        // SessionIds to Name
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, string>> SessionIdToName = new("SessionIdToName", "SessionIdToName", () => new Dictionary<string, string>() { });
            
        // Default session access level for new sessions
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<SessionAccessLevel> DefaultSessionAccessLevel = new("DefaultSessionAccessLevel", "Default Session Access Level", () => SessionAccessLevel.ContactsPlus);

        // Default session hidden status for new sessions
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> DefaultSessionHidden = new("DefaultSessionHidden", "Default Session Hidden", () => true);

        // secondary alpha for lists in messages float
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<float?> AlternateListAlpha = new("AlternateListAlpha", "Alternate List Alpha", () => 0.4f);

        // Disable autosave if no one is in world
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> SmartAutosaveEnabled = new("SmartAutosave", "Disable autosave if there are no users in current world", () => false);

        // ConfigSaved 
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> configSaved = new("_configSaved", "_configSaved", internalAccessOnly: true);




        public override void OnEngineInit()
        {
            config = GetConfiguration();
            // Initialize default values
            if (!config.TryGetValue(configSaved, out bool saved) || !saved)
            {
                config.Set(configSaved, true);
                config.Save(true);
            }
            Harmony harmony = new("me.New-Project-Final-Final-WIP.HeadlessTweaks");

            // Check if we are loaded by a headless client
            //Msg(typeof(FrooxEngine.Headless.HeadlessCommands).AssemblyQualifiedName);
            //isHeadless = Type.GetType("FrooxEngine.Headless.HeadlessCommands, Resonite") != null;
            //Msg($"Headless detected: {isHeadless}");

            // Sturdier check for if we're loaded by a headless client
            SystemInfo info = new();
            isHeadless = info.HeadDevice == FrooxEngine.HeadOutputDevice.Headless;

            // Check if the Discord namespace exists
            // If it does, we can assume that the Discord.NET library is installed
            // and we can init the Discord client
            if (config.GetValue(UseDiscordWebhook))
            {

                var typeString = "Discord.Webhook.DiscordWebhookClient, Discord.Net.Webhook";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                { // Windows for some reason requires the full identifier for discord.webhook but not for headless
                    typeString += ", Version = 3.12.0.0, Culture = neutral, PublicKeyToken = null";
                }

                Msg(typeString);
                isDiscordLoaded = Type.GetType(typeString) != null;

                if (isDiscordLoaded) {
                    Msg("Discord.NET library found");
                    DiscordIntegration.Init(harmony);
                }
                else Warn("Discord.NET library not found, but the UseDiscordWebhook option is enabled. Please put the Discord.NET library dlls in nml_libs to use this feature.");
            }

            // If we are not loaded by a headless client skip the rest
            if (!isHeadless)
            {
                Warn("Headless Not Detected! Skipping headless specific modules");
                return;
            }

            NewHeadlessCommands.Init(harmony);
            MessageCommands.Init();
            AutoInviteOptOut.Init(harmony);
            SmartAutosave.Init(harmony);
        }
    }
}