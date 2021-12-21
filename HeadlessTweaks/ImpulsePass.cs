using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using FrooxEngine.LogiX.ProgramFlow;
using FrooxEngine.LogiX;
using FrooxEngine;
using BaseX;
namespace HeadlessTweaks
{
    class ImpulsePass
    {
        public static void Init(Harmony harmony)
        {
            HeadlessTweaks.Msg("Initializing ImpulsePass");
            var dynamicImpulseTrigger = typeof(DynamicImpulseTrigger).GetMethod("Run");
            var dynamicImpulseTriggerPrefix = typeof(TypedImpulsePass).GetMethod("Prefix");

            harmony.Patch(dynamicImpulseTrigger, prefix: new HarmonyMethod(dynamicImpulseTriggerPrefix));
        }
        public static string TagTriggerPrefix = "$Headless.";
        [HarmonyPatch(typeof(DynamicImpulseTrigger), "Run")]
        class TypedImpulsePass
        {
            public static bool Prefix(DynamicImpulseTrigger __instance)
            {
                if (!__instance.Tag.Evaluate().StartsWith(TagTriggerPrefix)) return true;
                string TargetMethodName = __instance.Tag.Evaluate().Substring(TagTriggerPrefix.Length);

                var foundMethod = typeof(DynamicImpulseMethods).GetMethod(TargetMethodName);
                if (foundMethod == null)
                {
                    HeadlessTweaks.Msg("DynamicImpulseMethod '" + TargetMethodName + "' not found");
                    return true;
                }
                HeadlessTweaks.Msg("Found DynamicImpulseMethod '" + TargetMethodName + "'");
                Slot parameterSlot = __instance.TargetHierarchy.EvaluateRaw();
                
                foundMethod.Invoke(null, new object[] { __instance, parameterSlot });
                return false;
            }
        }

        public class DynamicImpulseMethods // *Public* methods are available to logix *:*)
        {
            public static void NameSlot(DynamicImpulseTrigger __instance, Slot parameterSlot)
            {
                Slot slot = FetchParameter<Slot>(parameterSlot, "Target");
                string name = FetchParameter<string>(parameterSlot, "Name");
             
                if (slot != null) slot.Name = name;
                return;
            }
        }
        [GenericTypes(GenericTypes.Group.NeosPrimitives, new System.Type[] { typeof(Slot), typeof(User) })]
        public static bool FetchParameter<T>(Slot slot, string variableName, out T value)
        {
            value = Coder<T>.Default;
            DynamicVariableSpace space = slot.FindSpace("HeadlessExec");
            if (space != null)
            {
                return space.TryReadValue<T>(variableName, out value);
            }
            return false;
        }
        public static T FetchParameter<T>(Slot slot, string variableName)
        {
            T value;
            if(FetchParameter<T>(slot, variableName, out value))
            {
                return value;
            }
            return Coder<T>.Default;
        }
    }
}
