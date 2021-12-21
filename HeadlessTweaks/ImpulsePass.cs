using HarmonyLib;
using FrooxEngine.LogiX.ProgramFlow;
using FrooxEngine;
namespace HeadlessTweaks
{
    class ImpulsePass
    {
        public static string TagTriggerPrefix = "$Headless.";


        public static void Init(Harmony harmony)
        {
            HeadlessTweaks.Msg("Initializing ImpulsePass");
            var dynamicImpulseTrigger = typeof(DynamicImpulseTrigger).GetMethod("Run");
            var dynamicImpulseTriggerPrefix = typeof(TypedImpulsePass).GetMethod("Prefix");

            harmony.Patch(dynamicImpulseTrigger, prefix: new HarmonyMethod(dynamicImpulseTriggerPrefix));
        }

        [HarmonyPatch(typeof(DynamicImpulseTrigger), "Run")]
        class TypedImpulsePass
        {
            public static bool Prefix(DynamicImpulseTrigger __instance)
            {
                if (!__instance.Tag.Evaluate().StartsWith(TagTriggerPrefix)) return true;
                string TargetMethodName = __instance.Tag.Evaluate().Substring(TagTriggerPrefix.Length);
                
                var foundMethod = typeof(ImpulsePassMethods).GetMethod(TargetMethodName);
                if (foundMethod == null)
                {
                    HeadlessTweaks.Msg("DynamicImpulseMethod '" + TargetMethodName + "' not found");
                    return true;
                }
                HeadlessTweaks.Msg("Found DynamicImpulseMethod '" + TargetMethodName + "'");
                Slot parameterSlot = __instance.TargetHierarchy.EvaluateRaw();
                
                foundMethod.Invoke(null, new object[] { __instance.World, parameterSlot });
                return false;
            }
        }
    }
}
