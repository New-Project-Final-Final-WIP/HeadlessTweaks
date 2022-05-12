using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FrooxEngine;
using CloudX.Shared;
using BaseX;
using NeosModLoader;
using System.Timers;

using static CloudX.Shared.MessageManager;
namespace HeadlessTweaks
{
    public static class HelperExtensions
    {
        //  Extend MessageManager.UserMessages with SendTextMessage(string, color)
        public static async void SendTextMessage(this UserMessages um, string text, color color)
        {
            string message = "<color=" + color.ToHexString(color.a != 1f) + ">" + text + "</color>";
            await um.SendTextMessage(message);
        }

        //  Extend MessageManager.UserMessages with SendObjectMessage(slot)
        public static async Task<bool> SendObjectMessage(this UserMessages um, Slot slot, Uri thumbnail = null, bool cleanUpSlot = true)
        {
            if (thumbnail == null) thumbnail = new Uri("neosdb:///141d9d5bf3041474d7dab6f09a7cd8e0fb6b480a750d1e3fd03549e3ba685d11.png");

            List<IItemThumbnailSource> componentsInChildren = slot.GetComponentsInChildren<IItemThumbnailSource>(
                s => s.HasThumbnail && s.Slot.IsActive, true);
            // Log if a component is found
            HeadlessTweaks.Msg("Found " + componentsInChildren.Count + " components in children of " + slot.Name);


            StaticTexture2D asset = null;
            if (componentsInChildren.Count == 0)
            {
                slot.RunSynchronously(() =>
                {
                    HeadlessTweaks.Msg("No thumbnail found in slot " + slot.Name);

                    asset = slot.World.AssetsSlot.AddSlot(slot.Name + "Thumbnail").AttachTexture(thumbnail);
                    var a = slot.AttachComponent<ItemTextureThumbnailSource>();
                    a.Texture.Target = asset;
                });
                // Delay to allow the thumbnail to be loaded
                HeadlessTweaks.Msg("Delaying to allow thumbnail to be loaded");
                await Task.Delay(200);
            }



            HeadlessTweaks.Msg("Sending object message...");
            var msgData = await um.CreateObjectMessage(slot, true);
            HeadlessTweaks.Msg("Object message created?:" + msgData);
            await msgData.uploadTask;
            HeadlessTweaks.Msg("Object message uploaded?");
            var success = await um.SendMessage(msgData.message);

            if (cleanUpSlot)
            {
                slot.RunSynchronously(() =>
                {
                    slot.Destroy();
                    if (asset != null) asset.Slot.Destroy();
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
                FrooxEngine.Record correspondingRecord = world.CorrespondingRecord;
                orb.URL = correspondingRecord?.URL;
            }
            orb.WorldName = world.Name;
            var thumbnailData = Userspace.GetThumbnailData(world);
            string thumbnail = null;
            if (thumbnailData != null)
            {
                ThumbnailInfo publicThumbnail = thumbnailData.PublicThumbnail;
                thumbnail = publicThumbnail?.Id;
            }
            if (thumbnail != null)
            {
                orb.ThumbnailTexURL = CloudXInterface.NeosThumbnailIdToHttp(thumbnail);
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
                    userMessages.SendTextMessage("Response timed out", color.Red);

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
    }
}
