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
        private static DiscordWebhookClient discordWebhook;
        public static DiscordWebhookClient DiscordWebhook { get => discordWebhook; private set => discordWebhook = value; }

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

            DiscordWebhook = new DiscordWebhookClient("https://discord.com/api/webhooks/" + HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookID) + "/" + HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookKey));

            var startSession = typeof(World).GetMethod("StartSession");
            var saveWorld = typeof(World).GetMethod("SaveWorld");

            var startPostfix = typeof(OtherDiscordEvents).GetMethod("Postfix");
            var savePostfix = typeof(SaveWorldDiscordEvents).GetMethod("Postfix");

            harmony.Patch(startSession, postfix: new HarmonyMethod(method: startPostfix));
            harmony.Patch(saveWorld, postfix: new HarmonyMethod(method: savePostfix));
            Engine.Current.OnShutdown += HeadlessEvents.HeadlessShutdown; // OnShutdownRequest

            Engine.Current.RunPostInit(() => {
                HeadlessEvents.HeadlessStartup();
                Engine.Current.WorldManager.WorldFailed += HeadlessEvents.WorldCrashed;
            });

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
        public static class HeadlessEvents
        {
            public static void WorldCreated(World world)
            {
                if (!EventHelper.IsEnabled(DiscordEvents.WorldCreated)) return;
                DiscordHelper.SendWorldEmbed(world, "Started", EventHelper.GetColor(DiscordEvents.WorldCreated), true);
                return;
            }
            public static void WorldSaved(World world)
            {
                if (!EventHelper.IsEnabled(DiscordEvents.WorldSaved)) return;
                DiscordHelper.SendWorldEmbed(world, "Saved", EventHelper.GetColor(DiscordEvents.WorldSaved));
                return;
            }
            public static void WorldDestroyed(World world)
            {
                if (!EventHelper.IsEnabled(DiscordEvents.WorldDestroyed)) return;
                DiscordHelper.SendWorldEmbed(world, "Closing", EventHelper.GetColor(DiscordEvents.WorldDestroyed));
                return;
            }
            public static void WorldCrashed(World world)
            {
                if (!EventHelper.IsEnabled(DiscordEvents.WorldCrashed)) return;
                DiscordHelper.SendWorldEmbed(world, "World Crashed", EventHelper.GetColor(DiscordEvents.WorldCrashed));
                return;
            }
            public static void UserJoined(User user)
            {
                if (user.IsHost)
                {
                    // World.WorldRunning is too early for any world name
                    // But getting the headless' session name requires too much jank due to the weird time it sets the name for startup sessions
                    WorldCreated(user.World);
                    if (user.HeadDevice == HeadOutputDevice.Headless) return;
                }
                if (!EventHelper.IsEnabled(DiscordEvents.UserJoin)) return;
                DiscordHelper.SendUserEmbed(user, "Joined", EventHelper.GetColor(DiscordEvents.UserJoin));
            }
            public static void UserLeft(User user)
            {
                if (!EventHelper.IsEnabled(DiscordEvents.UserLeft)) return;
                DiscordHelper.SendUserEmbed(user, "Left", EventHelper.GetColor(DiscordEvents.UserLeft));
            }
            public static void HeadlessStartup()
            {
                if (!EventHelper.IsEnabled(DiscordEvents.EngineStart)) return;
                DiscordHelper.SendStartEmbed(Engine.Current, "Headless [{1}] started with version {0}", EventHelper.GetColor(DiscordEvents.EngineStart));
            }
            public static void HeadlessShutdown()
            {
                if (!EventHelper.IsEnabled(DiscordEvents.EngineStop)) return;
                DiscordHelper.SendStartEmbed(Engine.Current, "Shutting down Headless [{1}]", EventHelper.GetColor(DiscordEvents.EngineStop));
            }
        }
        public class DiscordHelper 
        {
            public static async void SendMessage(string message = "null")
            {
                try
                {
                    message ??= "null";
                    await DiscordWebhook.SendMessageAsync(text: message, username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar, threadId: HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookThreadID));
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
                List<Embed> embedList = [];

                var embed = new EmbedBuilder()
                                .WithDescription(message)
                                .WithColor(color);

                embedList.Add(embed.Build());
                try
                {
                    await DiscordWebhook.SendMessageAsync(username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar, embeds: embedList, threadId: HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookThreadID));
                }
                catch (Exception e)
                {
                    ResoniteModLoader.ResoniteMod.Error(e.ToString());
                }
            }
            public static void SendStartEmbed(Engine engine, string action, Color color)
            {
                SendEmbed(string.Format(action, Engine.CurrentVersion, engine.LocalUserName), color);
            }
            public static void SendWorldEmbed(World world, string action, Color color, bool linkToSession = false)
            {
                string SessionName = world.RawName;
                // TODO Make this configurable 
                var mappings = HeadlessTweaks.SessionIdToName.GetValue();
                if (mappings.TryGetValue(world.SessionId, out string value))
                {
                    SessionName = value;
                }

                string message = string.Format("{0} [{1}] {2}", SessionName, world.HostUser.UserName, action);

                if (linkToSession && HeadlessTweaks.DiscordLinkToSession.GetValue()) {
                    // Components require Application owned webhooks, do able but don't feel like it right now
                    /*var builder = new ComponentBuilder().WithButton("Open Session", style: ButtonStyle.Link, url: Engine.Current.Cloud.ApiEndpoint + "/open/session/" + world.SessionId);
                    component = builder.Build();*/

                    message += $"\n-# [Join Session](<{Engine.Current.Cloud.ApiEndpoint + "/open/session/" + world.SessionId}>)";
                }
                SendEmbed(message, color);
            }
            public static async void SendUserEmbed(User user, string action, Color color, string userNameOverride = null, string userIdOverride = null)
            {
                var cloud = Engine.Current.Cloud;
                string userName = userNameOverride ?? user.UserName;
                string userId = userIdOverride ?? user.UserID;

                string SessionName = user.World.RawName;

                var mappings = HeadlessTweaks.SessionIdToName.GetValue();
                if (mappings.TryGetValue(user.World.SessionId, out string value))
                {
                    SessionName = value;
                }
                
                List<Embed> embedList = [];
                var embed = new EmbedBuilder
                {
                    // Embed property can be set within object initializer
                    Description = string.Format("{2} {0} [{1}]", SessionName, user.World.HostUser.UserName, action),
                    Color = color
                };
                SkyFrost.Base.User cloudUser = (await cloud.Users.GetUser(userId).ConfigureAwait(false))?.Entity;
                SkyFrost.Base.UserProfile profile = cloudUser?.Profile;


                Uri.TryCreate(profile?.IconUrl, UriKind.Absolute, out Uri userIconUri);
                if (userIconUri == null)
                {
                    userIconUri = OfficialAssets.Graphics.Thumbnails.AnonymousHeadset;
                }


                string userIcon = null;// = cloudUser?.Profile?.IconUrl;

                if (userIconUri != null && cloud.Assets.IsValidDBUri(userIconUri))
                { // Convert from NeosDB to Https if needed
                    userIcon = cloud.Assets.DBToHttp(userIconUri, SkyFrost.Base.DB_Endpoint.Default).AbsoluteUri;
                }

                string userUri = null;
                if (!string.IsNullOrWhiteSpace(userId)) userUri = cloud.ApiEndpoint + "/users/" + userId;

                embed.WithAuthor(name: userName, iconUrl: userIcon, url: userUri);
                //embed.WithCurrentTimestamp();
                embedList.Add(embed.Build());

                try
                {
                    await DiscordWebhook.SendMessageAsync(username: DiscordWebhookName, avatarUrl: DiscordWebhookAvatar, embeds: embedList, threadId: HeadlessTweaks.config.GetValue(HeadlessTweaks.DiscordWebhookThreadID));
                }
                catch (Exception e)
                {
                    HeadlessTweaks.Error(e.ToString());
                }
            }
        }




        class EventHelper
        {
            static readonly Dictionary<DiscordEvents, Color> DefaultColors = new(){
                { DiscordEvents.EngineStart, FromColorX(RadiantUI_Constants.Hero.CYAN)},
                { DiscordEvents.EngineStop, FromColorX(RadiantUI_Constants.Hero.RED) },
                { DiscordEvents.WorldCreated, FromColorX(RadiantUI_Constants.Hero.CYAN) },
                { DiscordEvents.WorldSaved, FromColorX(RadiantUI_Constants.Hero.YELLOW) },
                { DiscordEvents.WorldDestroyed, FromColorX(RadiantUI_Constants.Hero.ORANGE) },
                { DiscordEvents.WorldCrashed, FromColorX(RadiantUI_Constants.Hero.RED) },
                { DiscordEvents.UserJoin, FromColorX(RadiantUI_Constants.Hero.GREEN) },
                { DiscordEvents.UserLeft, FromColorX(RadiantUI_Constants.Hero.RED) },
            };

            public static Color GetColor(DiscordEvents name)
            {
                if (HeadlessTweaks.DiscordWebhookEventColors.GetValue().TryGetValue(name, out var color)) return FromColorX(color);
                
                return DefaultColors[name];
            }

            public static bool IsEnabled(DiscordEvents name)
            {
                if (HeadlessTweaks.DiscordWebhookEnabledEvents.GetValue().TryGetValue(name, out var enabled)) return enabled; 
                return true;
            }

            private static Color FromColorX(Elements.Core.colorX color)
            {
                var srgb = color.ToProfile(Elements.Core.ColorProfile.sRGB);
                return new Color(srgb.r, srgb.g, srgb.b);
            }
        }
        public enum DiscordEvents
        {
            EngineStart,
            HeadlessStart = 0,

            EngineStop,
            HeadlessStop = 1,

            WorldCreated,
            WorldSaved,
            WorldDestroyed,
            WorldCrashed,

            UserJoin,
            UserLeft,
        }
    }
}
