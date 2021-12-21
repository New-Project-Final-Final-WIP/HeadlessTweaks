using FrooxEngine;
using HarmonyLib;
using System;
using Discord;
using System.Collections.Generic;
using Discord.Webhook;
namespace HeadlessTweaks
{
    class DiscordIntegration
    {
        public static DiscordWebhookClient discordWebhook;

        public static void Init(Harmony harmony)
        {

            if (!HeadlessTweaks.config.UseDiscordWebhook) return;
            if (string.IsNullOrWhiteSpace(HeadlessTweaks.config.DiscordWebhookID) || string.IsNullOrWhiteSpace(HeadlessTweaks.config.DiscordWebhookKey))
            {
                HeadlessTweaks.Error("Webhook Key or Id is not defined in config");
                return;
            }
            HeadlessTweaks.Msg("Initializing DiscordIntegration");
            discordWebhook = new DiscordWebhookClient("https://discord.com/api/webhooks/" + HeadlessTweaks.config.DiscordWebhookID + "/" + HeadlessTweaks.config.DiscordWebhookKey);

            var startSession = typeof(World).GetMethod("StartSession");
            var saveWorld = typeof(World).GetMethod("SaveWorld");

            var startPostfix = typeof(OtherDiscordEvents).GetMethod("Postfix");
            var savePostfix = typeof(SaveWorldDiscordEvents).GetMethod("Postfix");

            harmony.Patch(startSession, postfix: new HarmonyMethod(startPostfix));
            harmony.Patch(saveWorld, postfix: new HarmonyMethod(savePostfix));
            Engine.Current.OnShutdown += HeadlessEvents.HeadlessShutdown;

            //HeadlessEvents.HeadlessStartup(); too early?
        }
        [HarmonyPatch(typeof(World), "SaveWorld")]
        class SaveWorldDiscordEvents
        {
            public static void Postfix(World __instance, ref SavedGraph __result)
            {
                HeadlessEvents.WorldSaved(__instance);
                return;
            }
        }
        [HarmonyPatch(typeof(World), "StartSession")]
        class OtherDiscordEvents
        {
            public static void Postfix(World __instance, ref World __result)
            {
                __result.UserJoined += HeadlessEvents.UserJoined;
                __result.UserLeft += HeadlessEvents.UserLeft;
                __result.WorldDestroyed += HeadlessEvents.WorldDestroyed;
                return;
            }
        }
        class HeadlessEvents
        {
            public static void WorldCreated(World world)
            {
                EmbedHelper.sendWorldMessage(world, "Started", new Color(r: 0.0f, g: 0.8f, b: 0.8f));
                return;
            }
            public static void WorldSaved(World world)
            {
                EmbedHelper.sendWorldMessage(world, "Saved", new Color(r: 0.8f, g: 0.8f, b: 0.0f));
                return;
            }
            public static void WorldDestroyed(World world)
            {
                EmbedHelper.sendWorldMessage(world, "Closing", new Color(r: 0.8f, g: 0.2f, b: 0.0f));
                return;
            }
            public static void UserJoined(User user)
            {
                if (user.IsHost)
                {
                    WorldCreated(user.World);

                    if (user.World.SessionId == "S-U-New-Headless:New_Scene")
                    {
                        EmbedHelper.sendUserMessage(user, "Joined", new Color(r: 0.0f, g: 0.8f, b: 0.0f), bernie: true);
                    }
                    if (user.HeadDevice == HeadOutputDevice.Headless) return;
                }
                EmbedHelper.sendUserMessage(user, "Joined", new Color(r: 0.0f, g: 0.8f, b: 0.0f));
            }
            public static void UserLeft(User user)
            {
                EmbedHelper.sendUserMessage(user, "Left", new Color(r: 0.8f, g: 0.0f, b: 0.0f));
            }
            public static void HeadlessStartup()
            {
                EmbedHelper.sendHeadlessMessage(Engine.Current, "Headless [{1}] started with version {0}", new Color(r: 0.0f, g: 0.0f, b: 1.0f));
            }
            public static void HeadlessShutdown()
            {

                EmbedHelper.sendHeadlessMessage(Engine.Current, "Shutting down Headless [{1}]", new Color(r: 1.0f, g: 0.0f, b: 0.0f));
            }
        }
        class EmbedHelper 
        {
            public static void sendHeadlessMessage(Engine engine, String action, Color color)
            {
                List<Embed> embedList = new List<Embed>();
                var embed = new EmbedBuilder
                {
                    Description = String.Format(action, engine.VersionString, engine.LocalUserName),
                    Color = color
                };
                embedList.Add(embed.Build());
                discordWebhook.SendMessageAsync(username: HeadlessTweaks.config.DiscordWebhookUsername, avatarUrl: HeadlessTweaks.config.DiscordWebhookAvatar, embeds: embedList);
            }
            public static void sendWorldMessage(World world, String action, Color color)
            {
                string SessionName = world.Name;
                if (world.SessionId == "S-U-New-Headless:New_Scene") SessionName = "New Scene";
                List<Embed> embedList = new List<Embed>();
                var embed = new EmbedBuilder
                {
                    // Embed property can be set within object initializer
                    //Title = "Session " + action,
                    Description = String.Format("{0} [{1}] {2}", SessionName, world.HostUser.UserName, action),
                    Color = color
                };
                //embed.WithCurrentTimestamp();
                embedList.Add(embed.Build());
                discordWebhook.SendMessageAsync(username: HeadlessTweaks.config.DiscordWebhookUsername, avatarUrl: HeadlessTweaks.config.DiscordWebhookAvatar, embeds: embedList);
            }
            public static async void sendUserMessage(User user, String action, Color color, bool bernie = false)
            {
                string userName = user.UserName;
                string userId = user.UserID;
                if (bernie)
                {
                    userName = "Bernie Sanders";
                    userId = "U-Bernie-Sanders";
                }
                string SessionName = user.World.Name;
                if (user.World.SessionId == "S-U-New-Headless:New_Scene") SessionName = "New Scene";
                List<Embed> embedList = new List<Embed>();
                var embed = new EmbedBuilder
                {
                    // Embed property can be set within object initializer
                    Description = String.Format("{2} {0} [{1}]", SessionName, user.World.HostUser.UserName, action),
                    Color = color
                };
                CloudX.Shared.User cloudUser = (await user.Cloud.GetUser(userId).ConfigureAwait(false))?.Entity;
                string userIcon = cloudUser?.Profile?.IconUrl;
                if (CloudX.Shared.CloudXInterface.IsValidNeosDBUri(new Uri(userIcon))) // a
                {
                    userIcon = CloudX.Shared.CloudXInterface.NeosDBToHttp(new Uri(userIcon), CloudX.Shared.NeosDB_Endpoint.CDN).AbsoluteUri;
                }
                string userUri = null;
                if (!string.IsNullOrWhiteSpace(userId)) userUri = CloudX.Shared.CloudXInterface.NEOS_API + "/api/users/" + userId;

                embed.WithAuthor(name: userName, iconUrl: userIcon, url: userUri);
                //embed.WithCurrentTimestamp();
                embedList.Add(embed.Build());
                await discordWebhook.SendMessageAsync(username: HeadlessTweaks.config.DiscordWebhookUsername, avatarUrl: HeadlessTweaks.config.DiscordWebhookAvatar, embeds: embedList);
            }
        }
    }
}
