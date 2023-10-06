using FrooxEngine;
using HarmonyLib;
using System;
using Discord;
using System.Collections.Generic;
using Discord.Webhook;

namespace HeadlessTweaks
{
    public class DiscordIntegration
    {
        public static DiscordWebhookClient discordWebhook;

        public static string DiscordWebhookName
        {
            get
            {
                var username = HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookUsername);

                // if the username is empty, use the default username
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = Engine.Current.LocalUserName;
                }
                return username;
            }
        }
        
        public static string DiscordWebhookAvatar
        {
            get
            {
                var avatarUrl = HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookAvatar);

                // if the avatarUrl is empty, use the default avatarUrl
                if (string.IsNullOrWhiteSpace(avatarUrl))
                {
                    avatarUrl = Engine.Current.Cloud?.CurrentUser?.Profile?.IconUrl;
                }

                Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri uri);

                if(uri == null) uri = OfficialAssets.Graphics.Thumbnails.AnonymousHeadset;
                

                if (Engine.Current.Cloud.Assets.IsValidDBUri(uri))
                { // Convert from NeosDB to Https if needed
                    uri = Engine.Current.Cloud.Assets.DBToHttp(uri, SkyFrost.Base.DB_Endpoint.Default);
                }
                avatarUrl = uri.ToString();
                return avatarUrl;
            }
        }

        public static void Init(Harmony harmony)
        {
            if (!HeadlessTweaks.config.GetValue(HeadlessTweaks.UseDiscordWebhook)) return;
            if (string.IsNullOrWhiteSpace(HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookID)) || string.IsNullOrWhiteSpace(HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookKey)))
            {
                HeadlessTweaks.Error("Webhook Key or Id is not defined in config");
                return;
            }
            
            discordWebhook = new DiscordWebhookClient("https://discord.com/api/webhooks/" + HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookID) + "/" + HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookKey));

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
            public static void Postfix(World __instance)
            {
                HeadlessEvents.WorldSaved(__instance);
                return;
            }
        }
        [HarmonyPatch(typeof(World), "StartSession")]
        class OtherDiscordEvents
        {
            public static void Postfix(ref World __result)
            {
                __result.UserJoined += HeadlessEvents.UserJoined;
                __result.UserLeft += HeadlessEvents.UserLeft;
                __result.WorldDestroyed += HeadlessEvents.WorldDestroyed;
                return;
            }
        }
        public class HeadlessEvents
        {
            public static void WorldCreated(World world)
            {
                DiscordHelper.SendWorldEmbed(world, "Started", new Color(r: 0.0f, g: 0.8f, b: 0.8f));
                return;
            }
            public static void WorldSaved(World world)
            {
                DiscordHelper.SendWorldEmbed(world, "Saved", new Color(r: 0.8f, g: 0.8f, b: 0.0f));
                return;
            }
            public static void WorldDestroyed(World world)
            {
                DiscordHelper.SendWorldEmbed(world, "Closing", new Color(r: 0.8f, g: 0.2f, b: 0.0f));
                return;
            }
            public static void UserJoined(User user)
            {
                if (user.IsHost)
                {
                    WorldCreated(user.World);
                    if (user.HeadDevice == HeadOutputDevice.Headless) return;
                }
                DiscordHelper.SendUserEmbed(user, "Joined", new Color(r: 0.0f, g: 0.8f, b: 0.0f));
            }
            public static void UserLeft(User user)
            {
                DiscordHelper.SendUserEmbed(user, "Left", new Color(r: 0.8f, g: 0.0f, b: 0.0f));
            }
            public static void HeadlessStartup()
            {
                DiscordHelper.SendStartEmbed(Engine.Current, "Headless [{1}] started with version {0}", new Color(r: 0.0f, g: 0.0f, b: 1.0f));
            }
            public static void HeadlessShutdown()
            {

                DiscordHelper.SendStartEmbed(Engine.Current, "Shutting down Headless [{1}]", new Color(r: 1.0f, g: 0.0f, b: 0.0f));
            }
        }
        public class DiscordHelper 
        {
            public static async void SendMessage(string message)
            {
                try
                {
                    await discordWebhook.SendMessageAsync(text: message, username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar);
                }
                catch (Exception e)
                {
                    ResoniteModLoader.ResoniteMod.Error(e.ToString());
                }
            }


            public static void SendEmbed(string message, Elements.Core.colorX color)
            {
                SendEmbed(message, new Color(color.r, color.g, color.b));
            }

            
            public static async void SendEmbed(string message, Color color)
            {
                List<Embed> embedList = new();
                var embed = new EmbedBuilder
                {
                    Description = message,
                    Color = color
                };
                embedList.Add(embed.Build());
                try
                {
                    await discordWebhook.SendMessageAsync(username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar, embeds: embedList);
                }
                catch (Exception e)
                {
                    ResoniteModLoader.ResoniteMod.Error(e.ToString());
                }
            }
            public static void SendStartEmbed(Engine engine, string action, Color color)
            {
                SendEmbed(string.Format(action, engine.VersionString, engine.LocalUserName), color);
            }
            public static void SendWorldEmbed(World world, string action, Color color)
            {
                string SessionName = world.RawName;
                // TODO: Make this configurable 
                var mappings = HeadlessTweaks.SessionIdToName.GetValue();
                if (mappings.ContainsKey(world.SessionId))
                {
                    SessionName = mappings[world.SessionId];
                }
                SendEmbed(string.Format("{0} [{1}] {2}", SessionName, world.HostUser.UserName, action), color);
            }
            public static async void SendUserEmbed(User user, string action, Color color, string userNameOverride = null, string userIdOverride = null)
            {
                string userName = userNameOverride ?? user.UserName;
                string userId = userIdOverride ?? user.UserID;

                string SessionName = user.World.RawName;

                var mappings = HeadlessTweaks.SessionIdToName.GetValue();
                if (mappings.ContainsKey(user.World.SessionId))
                {
                    SessionName = mappings[user.World.SessionId];
                }
                
                List<Embed> embedList = new();
                var embed = new EmbedBuilder
                {
                    // Embed property can be set within object initializer
                    Description = string.Format("{2} {0} [{1}]", SessionName, user.World.HostUser.UserName, action),
                    Color = color
                };
                SkyFrost.Base.User cloudUser = (await user.Cloud.Users.GetUser(userId).ConfigureAwait(false))?.Entity;
                SkyFrost.Base.UserProfile profile = cloudUser?.Profile;


                Uri.TryCreate(profile?.IconUrl, UriKind.Absolute, out Uri userIconUri);
                if (userIconUri == null)
                {
                    userIconUri = OfficialAssets.Graphics.Thumbnails.AnonymousHeadset;
                }


                string userIcon = null;// = cloudUser?.Profile?.IconUrl;
                // if(userIcon!=null)
                if (userIconUri != null && user.Cloud.Assets.IsValidDBUri(userIconUri))
                { // Convert from NeosDB to Https if needed
                    userIcon = user.Cloud.Assets.DBToHttp(userIconUri, SkyFrost.Base.DB_Endpoint.Default).AbsoluteUri;
                }

                string userUri = null;
                if (!string.IsNullOrWhiteSpace(userId)) userUri = user.Cloud.ApiEndpoint + "/api/users/" + userId;

                embed.WithAuthor(name: userName, iconUrl: userIcon, url: userUri);
                //embed.WithCurrentTimestamp();
                embedList.Add(embed.Build());

                try
                {
                    await discordWebhook.SendMessageAsync(username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar, embeds: embedList);
                }
                catch (Exception e)
                {
                    HeadlessTweaks.Error(e.ToString());
                }
            }
        }
    }
}
