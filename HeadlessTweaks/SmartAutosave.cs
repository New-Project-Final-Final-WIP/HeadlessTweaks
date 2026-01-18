using FrooxEngine;
using FrooxEngine.Headless;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HeadlessTweaks
{
    public class SmartAutosave
    {
        internal static void Init(Harmony harmony)
        {
            // Thank you zonni from stack overflow for finally teaching me AsyncStateMachineAttribute exists

            // Get the existing HandlerLoop method from WorldHandler
            var handlerLoopMethod = typeof(WorldHandler).GetMethod("HandlerLoop", BindingFlags.Instance | BindingFlags.NonPublic);
            // Because it is async we want to get it's state machine, we can find this by it's AsyncStateMachineAttribute
            var stateMachineAttr = handlerLoopMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
            // And from that state machine we can get the generated type and it's MoveNext method
            var moveNextMethod = stateMachineAttr.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);

            // Get our transpiler function
            var transpiler = typeof(SmartAutosave).GetMethod(nameof(Transpile));

            // Patch world handler's handler loop
            harmony.Patch(moveNextMethod, transpiler: new(transpiler));
        }

        /// <summary>
        /// This allows saving once more after players have left the session
        /// </summary>
        static readonly Dictionary<World, bool> ShouldSaveOnceMore = [];

        static readonly MethodInfo CanSaveMethod = typeof(Userspace).GetMethod(nameof(Userspace.CanSave));
        static readonly MethodInfo ShouldSaveMethod = typeof(SmartAutosave).GetMethod(nameof(ShouldAutoSave));


        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            // Loop over every instruction and return them in order
            foreach (var instruction in instructions)
            {
                // Find the call for Userspace.CanSave inside of handler loop and replace it with our ShouldAutoSave method
                // This should only be in the if check for auto saving
                if(instruction.Calls(CanSaveMethod))
                {
                    yield return new(OpCodes.Call, ShouldSaveMethod);
                    continue;
                }

                // Return the original instruction
                yield return instruction;
            }
        }

        public static bool ShouldAutoSave(World world)
        {
            // If we can't save just return false here and don't continue
            if (!Userspace.CanSave(world)) return false;

            // If our save patch is disabled continue to save the world and skip the rest
            if (!HeadlessTweaks.SmartAutosaveEnabled.GetValue()) return true;

            // Ensure there is an entry for this world in the list
            ShouldSaveOnceMore.TryAdd(world, false);

            // If there is more than just the host in the world then autosave should run
            if(world.UserCount > 1 || ShouldSaveOnceMore[world])
            {
                // If there are currently players in the session (asside from host)
                // Then we should save again next time even if there are no more players
                ShouldSaveOnceMore[world] = world.UserCount > 1;
                
                // Continue to save the world
                return true;
            }

            // Don't save the world
            return false;
        }
    }
}
