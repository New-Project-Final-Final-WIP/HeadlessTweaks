using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using BaseX;
using System.IO;
using FrooxEngine.UIX;
using System.Threading;

namespace HeadlessTweaks
{
    class LogOutputDisplay
    {
        private static List<Slot> logOutputs = new List<Slot>();

        internal static void Init(Harmony harmony)
        {
            UniLog.OnLog += onLog;
            UniLog.OnError += onError;
        }
        private static void log(string message, color color)
        {
            if (message.Contains("[DONTLOG]")) return; // So there is no feedback loop
            var reader = new StringReader(message); //;-;
            var newMessage = $"<color={color.ToHexString()}>" + message.Insert(reader.ReadLine().Length,"</color>");

            updateLogOutputs(newMessage);
        }

        private static void updateLogOutputs(string newMessage)
        {
            foreach (var logOutput in logOutputs)
            {
                logOutput.World.RunSynchronously(() =>
                {
                    Slot content = Utilities.FetchParameter<Slot>(logOutput, "Content");
                    if (content == null) return;
                    UIBuilder ui = new UIBuilder(content);
                    Text text = ui.Text(newMessage, alignment: Alignment.TopLeft);
                    text.Color.Value = color.White;
                    text.VerticalAutoSize.Value = false;
                });
            }
        }
        private static void onError(string obj)
        {
            log(obj, color.Red);
        }

        private static void onLog(string obj)
        {
            log(obj, color.Cyan);
        }
        public static void removeLogOutput(Worker worker)
        {
            removeLogOutput(worker as Slot);
        }
        public static void removeLogOutput(Slot slot)
        {
            if (slot == null || !logOutputs.Contains(slot)) return;
            logOutputs.Remove(slot);
            Utilities.TrySetParameter<bool>(slot, "_isRegistered", false);
            HeadlessTweaks.Msg($"[{slot.World.Name}] Unregistered LogOutput: '{slot.Name}'");
        }
        public static void addLogOutput(Slot slot)
        {
            if (slot == null || logOutputs.Contains(slot)) return;
            logOutputs.Add(slot);
            slot.Disposing += LogOutputDisplay.removeLogOutput;

            Utilities.TrySetParameter<bool>(slot, "_isRegistered", true);
            HeadlessTweaks.Msg($"[{slot.World.Name}] Registered new LogOutput: '{slot.Name}'");
        }
        private static void Debug(string message)
        { // So there is no feedback loop
            HeadlessTweaks.Msg("[DONTLOG] " + message);
        }
    }
}
