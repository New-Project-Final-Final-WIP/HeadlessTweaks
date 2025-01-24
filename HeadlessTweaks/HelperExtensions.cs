using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FrooxEngine;
using SkyFrost.Base;
using Elements.Core;
using ResoniteModLoader;
using System.Timers;
using FrooxEngine.Store;

namespace HeadlessTweaks
{
    public static class HelperExtensions
    {
        //  Extend MessageManager.UserMessages with SendTextMessage(string, color)
        public static async Task SendTextMessage(this UserMessages um, string text, colorX color)
        {
            string message = text;
            if (color != RadiantUI_Constants.TEXT_COLOR)
                 message = "<color=" + color.ToHexString(color.a != 1f) + ">" + text + "</color>";
            await um.SendTextMessage(message);
        }

        //  Extend MessageManager.UserMessages with SendObjectMessage(slot)
        public static async Task<bool> SendObjectMessage(this UserMessages userMessages, Slot slot, Uri thumbnail = null, bool cleanUpSlot = true)
        {
            if (thumbnail == null) thumbnail = OfficialAssets.Graphics.Icons.Dash.Folder;


            List<IItemThumbnailSource> componentsInChildren = slot.GetComponentsInChildren<IItemThumbnailSource>(
                s => s.HasThumbnail && s.Slot.IsActive, true);

            StaticTexture2D asset = null;
            if (componentsInChildren.Count == 0)
            {
                HeadlessTweaks.Msg($"No thumbnail found in slot {slot.Name}, attaching thumbnail");
                
                await slot.World.Coroutines.StartTask(async () =>
                {
                    asset = slot.World.AssetsSlot.AddSlot(slot.Name + "Thumbnail").AttachTexture(thumbnail);
                    var a = slot.AttachComponent<ItemTextureThumbnailSource>();
                    a.Texture.Target = asset;

                    await new NextUpdate();
                    await new NextUpdate();
                    await new ToBackground();
                });
                
                bool loaded = await asset.RawAsset.WaitForLoad();

                if (!loaded)
                {
                    HeadlessTweaks.Warn("Thumbnail failed to load, canceling as headlesses can't render thumbnails");
                    await userMessages.SendTextMessage("Thumbnail failed to load, canceling as headlesses can't render thumbnails");
                    return false;
                }

            }

            var msgData = await userMessages.CreateObjectMessage(slot, true);

            await msgData.uploadTask;

            var success = await userMessages.SendMessage(msgData.message);

            if (cleanUpSlot)
            {
                slot.RunSynchronously(() =>
                {
                    slot.Destroy();
                    asset?.Slot.Destroy();
                });
            }
            return success;
        }

        // World.GetOrb(bool sessionOrb)
        public static Slot GetOrb(this World world, bool sessionOrb)
        {
            HeadlessTweaks.Msg("Getting orb...");
            Slot root = world.AddLocalSlot("World Orb", true);
            WorldOrb orb = root.AttachComponent<WorldOrb>();
            if (sessionOrb)
            {
                orb.ActiveSessionURLs = world.SessionURLs;
                orb.ActiveUsers.Value = world.UserCount;
            }
            else
            {
                FrooxEngine.Store.Record correspondingRecord = world.CorrespondingRecord;
                orb.URL = correspondingRecord?.GetUrl(Engine.Current.PlatformProfile);
            }
            orb.WorldName = world.Name;
            //world.HostUser.ThumbnailUrl
            var thumbnailData = Userspace.GetThumbnailData(world);
            if (thumbnailData != null && thumbnailData.PublicThumbnail != null) {
                orb.ThumbnailTexURL = Engine.Current.Cloud.Assets.ThumbnailToHttp(thumbnailData.PublicThumbnail);
            }
            HeadlessTweaks.Msg("Orb created");
            return root;
        }

        public static T GetValue<T>(this ModConfigurationKey<T> key)
        {
            return HeadlessTweaks.config.GetValue(key);
        }
        public static void SetValue<T>(this ModConfigurationKey<T> key, T value, string eventLabel = null)
        {
            HeadlessTweaks.config.Set(key, value, eventLabel);
        }
        public static void SetValueAndSave<T>(this ModConfigurationKey<T> key, T value, string eventLabel = null)
        {
            HeadlessTweaks.config.Set(key, value, eventLabel);
            HeadlessTweaks.config.Save();
        }


        // Wait for response using tastcompletionsoruce
        // Add a task completion source to the dictionary MessageCommands.responseTasks

        public static async Task<Message> WaitForResponse(this UserMessages userMessages, double timeLimit = 0)
        {
            var tcs = new TaskCompletionSource<Message>();
            if (timeLimit > 0)
            {
                var timer = new Timer(timeLimit * 1000);
                timer.Elapsed += (sender, e) =>
                {
                    // Check if the task is already completed 
                    // If so clean up timer and return
                    if (tcs.Task.IsCompleted)
                    {
                        timer.Dispose();
                        return;
                    }

                    // Send the user a message to let them know the response has timed out
                    _ = userMessages.SendTextMessage("Response timed out", RadiantUI_Constants.Hero.RED);

                    // cancel the task
                    tcs.TrySetResult(null);

                    MessageCommands.responseTasks.Remove(userMessages);
                    timer.Dispose();
                };
                timer.Start();
            }
            MessageCommands.responseTasks.Add(userMessages, tcs);

            var message = await tcs.Task;
            return message;
        }

        // Request an object message from the user

        public static async Task<FrooxEngine.Store.Record> RequestObjectMessage(this UserMessages userMessages, string message, double timeLimit = 45)
        {
            _ = userMessages.SendTextMessage(message);

            var response = await userMessages.WaitForResponse(timeLimit);

            if (response == null)
                return null;

            // Check if response type is an item
            if (response.MessageType != SkyFrost.Base.MessageType.Object)
            {
                _ = userMessages.SendTextMessage("Invalid response");
                return null;
            }
            // Extract the record from the message
            return response.ExtractContent<FrooxEngine.Store.Record>();
        }

        public static async Task<string> RequestTextMessage(this UserMessages userMessages, string message, double timeLimit = 45)
        {
            _ = userMessages.SendTextMessage(message);

            var response = await userMessages.WaitForResponse(timeLimit);

            if (response == null)
                return null;

            // Check if response type is an item
            if (response.MessageType != SkyFrost.Base.MessageType.Text)
            {
                _ = userMessages.SendTextMessage("Invalid response");
                return null;
            }
            // Extract the record from the message
            return response.Content;
        }


        public static Contact ToContact(this SkyFrost.Base.User user) => new()
        {
            ContactUsername = user.Username,
            AlternateUsernames = user.AlternateNormalizedNames,
            ContactUserId = user.Id,
            ContactStatus = ContactStatus.SearchResult,
            Profile = user.Profile
        };


        public static async Task<bool> WaitForLoad(this IAsset asset)
        {
            if (asset == null)
                return false;
            
            while (true)
            {
                if (asset == null || asset.LoadState == AssetLoadState.Failed) return false;

                if (asset.LoadState == AssetLoadState.FullyLoaded) return true;

                await Task.Delay(100);
            }
        }
    }
}
