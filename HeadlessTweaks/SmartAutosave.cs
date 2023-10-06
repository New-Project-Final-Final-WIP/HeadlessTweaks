using HarmonyLib;
using System;
using System.Collections.Generic;
using FrooxEngine;
using System.Diagnostics;

namespace HeadlessTweaks
{
    public class SmartAutosave
    {
        internal static void Init(Harmony harmony)
        {
            var target = typeof(Userspace).GetMethod(nameof(Userspace.CanSave));
            //var target = typeof(Userspace).GetMethod("WorldSaveAuto");
            var prefix = typeof(SmartAutosave).GetMethod(nameof(CanSaveOverride));

            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        static Type handlerLoop;

        // A map of worlds to booleans 
        static readonly Dictionary<World, bool> dontSave = new();
        
        
        public static bool CanSaveOverride(World world, ref bool __result)
        {
            if (!HeadlessTweaks.SmartAutosaveEnabled.GetValue())
                return true;

            if (!dontSave.ContainsKey(world))
                dontSave.Add(world, false);
            
            // Awful
            // Get call stack
            StackTrace stackTrace = new();
            // Get calling method name
            var frame = stackTrace.GetFrame(2);
            var decType = frame.GetMethod().DeclaringType;
            
            if (handlerLoop == null && decType.Name.Contains("HandlerLoop"))
                handlerLoop = decType;

            if (handlerLoop != decType || world.UserCount > 1)
            {
                //ResoniteMod.Msg("Try to save");

                if(world.UserCount > 1 && dontSave[world])
                {
                    dontSave[world] = false;
                }
                
                return true;
            }
            

            if (!dontSave[world])
            {
                //ResoniteMod.Msg("Last save");
                dontSave[world] = true;
                return true;
            }

            //ResoniteMod.Msg("Dont try to save");
            __result = false;
            return false;
        }
    }
}
