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
        public static readonly ModConfigurationKey<bool> UseImpulsePass = new ModConfigurationKey<bool>("UseImpulsePass", "Use Impulse Pass", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> UseLogDisplay = new ModConfigurationKey<bool>("UseLogDisplay", "Use LogDisplay", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<bool> UseDiscordWebhook = new ModConfigurationKey<bool>("UseDiscordWebhook", "Use Discord Webhook", () => false);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookID = new ModConfigurationKey<string>("DiscordWebhookID", "Discord Webhook ID", () => null);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookKey = new ModConfigurationKey<string>("DiscordWebhookKey", "Discord Webhook Key", () => null);
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookUsername = new ModConfigurationKey<string>("DiscordWebhookUsername", "Discord Webhook Username", () => "New Headless");
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<string> DiscordWebhookAvatar = new ModConfigurationKey<string>("DiscordWebhookAvatar", "Discord Webhook Avatar", () => "https://newweb.page/assets/images/logo.png");
        
        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<List<string>> AutoInviteOptOut = new ModConfigurationKey<List<string>>("AutoInviteOptOut", "Auto Invite Opt Out", () => new List<string>(), internalAccessOnly: true);

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, PermissionLevel>> PermissionLevels = new ModConfigurationKey<Dictionary<string, PermissionLevel>>("PermissionLevels", "Permission Levels", () => new Dictionary<string, PermissionLevel>());

        [AutoRegisterConfigKey]
        public static readonly ModConfigurationKey<Dictionary<string, string>> WorldRoster = new ModConfigurationKey<Dictionary<string, string>>("WorldRoster", "World Roster", () => new Dictionary<string, string>());






        public override void OnEngineInit()
        {
            config = GetConfiguration();
            

            Harmony harmony = new Harmony("me.New-Project-Final-Final-WIP.HeadlessTweaks");

            Type neosHeadless = AccessTools.TypeByName("NeosHeadless.Program");//Type.GetType("NeosHeadless.Program");
            isHeadless = neosHeadless != null;
            Msg("Headless: " + isHeadless);
            Msg("HeadlessType: " + neosHeadless);

            if (config.GetValue(UseDiscordWebhook)) DiscordIntegration.Init(harmony);
            if (config.GetValue(UseImpulsePass)) ImpulsePass.Init(harmony);
            if (config.GetValue(UseLogDisplay)) LogOutputDisplay.Init(harmony);

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