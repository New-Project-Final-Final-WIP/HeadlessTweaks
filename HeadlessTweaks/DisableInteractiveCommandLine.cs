using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using FrooxEngine.Headless;

using HarmonyLib;

namespace HeadlessTweaks
{
    /// <summary>
    /// A patch that prevents the headless interactive input prompt from running.
    /// </summary>
    static class DisableInteractiveCommandLine
    {
        /// <summary>
        /// <see cref="ConditionalWeakTable{TKey, TValue}"/> of seen <see cref="CommandHandler"/> instances to <see cref="TaskCompletionSource"/>s that asynchronously mimic the behavior of their "stopProcessing" flag. MUST be <see langword="lock"/>ed before usage.
        /// </summary>
        static readonly ConditionalWeakTable<CommandHandler, TaskCompletionSource> StopProcessingFlags = new ConditionalWeakTable<CommandHandler, TaskCompletionSource>();

        /// <summary>
        /// Applies the <see cref="DisableInteractiveCommandLine"/> patch.
        /// </summary>
        /// <param name="harmony">The <see cref="Harmony"/> instance to use for patching.</param>
        public static void Init(Harmony harmony)
        {
            HeadlessTweaks.Msg("Applying non-interative command line patch...");

            var thisType = typeof(DisableInteractiveCommandLine);
            var processCommandsPrefix = thisType.GetMethod(nameof(ProcessCommandsPrefix), BindingFlags.NonPublic | BindingFlags.Static);
            var stopProcessingPostfix = thisType.GetMethod(nameof(StopProcessingPostfix), BindingFlags.NonPublic | BindingFlags.Static);

            var commandHandlerType = typeof(CommandHandler);
            var processCommands = commandHandlerType.GetMethod(nameof(CommandHandler.ProcessCommands));
            var stopProcessing = commandHandlerType.GetMethod(nameof(CommandHandler.StopProcessing));

            harmony.Patch(processCommands, prefix: new HarmonyMethod(processCommandsPrefix));
            harmony.Patch(stopProcessing, postfix: new HarmonyMethod(stopProcessingPostfix));
        }

        /// <summary>
        /// Patch for <see cref="CommandHandler.ProcessCommands"/> such that it just returns a <see cref="Task"/> that will complete when <see cref="CommandHandler.StopProcessing"/>.
        /// Requires the <see cref="StopProcessingPostfix(CommandHandler)"/> patch to work.
        /// </summary>
        /// <param name="__instance">The executing <see cref="CommandHandler"/> instance.</param>
        /// <param name="__result">The <see cref="Task"/> result of <see cref="CommandHandler.ProcessCommands"/>.</param>
        /// <returns><see langword="false"/> to prevent the original implementation from running.</returns>
        static bool ProcessCommandsPrefix(CommandHandler __instance, ref Task __result)
        {
            HeadlessTweaks.Msg("Interative command line requested, skipping...");
            if (!GetOrCreateTcs(__instance, out var flag))
                HeadlessTweaks.Error(
                    $"By the time {nameof(ProcessCommandsPrefix)} ran, there was already a {nameof(TaskCompletionSource)} for the registered instance! Have the mod authors look at this as something has changed under the hood!");

            __result = flag.Task;
            return false;
        }

        /// <summary>
        /// Patch for <see cref="CommandHandler.StopProcessing"/> such that, in addition to setting the "stopProcessing" flag, it also completes the <see cref="Task"/>
        /// that might have been registered in <see cref="StopProcessingFlags"/> by <see cref="ProcessCommandsPrefix(CommandHandler, ref Task)"/>.
        /// </summary>
        /// <param name="__instance">The executing <see cref="CommandHandler"/> instance.</param>
        static void StopProcessingPostfix(CommandHandler __instance)
        {
            HeadlessTweaks.Msg("Interative command line stop requested, completing Task...");
            if (GetOrCreateTcs(__instance, out var flag))
                HeadlessTweaks.Error(
                    $"{nameof(CommandHandler.StopProcessing)} was called before the {nameof(ProcessCommandsPrefix)} for a {nameof(CommandHandler)} instance! Have the mod authors look at this as something has changed under the hood!");

            if (!flag.TrySetResult())
                HeadlessTweaks.Warn($"StopProcessing was called multiple times for the same {nameof(CommandHandler)} instance? Not a problem, but curious");
        }

        /// <summary>
        /// Create a new <see cref="TaskCompletionSource"/> for usage with <see cref="StopProcessingFlags"/>.
        /// </summary>
        /// <param name="instance">The executing <see cref="CommandHandler"/> instance.</param>
        /// <param name="taskCompletionSource">The <see cref="TaskCompletionSource"/> to use for the <paramref name="instance"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="taskCompletionSource"/> was created as a result of this call, <see langword="false"/> otherwise.</returns>
        static bool GetOrCreateTcs(CommandHandler instance, out TaskCompletionSource taskCompletionSource)
        {
            bool result;
            lock (StopProcessingFlags)
            {
                result = !StopProcessingFlags.TryGetValue(instance, out taskCompletionSource);
                if (result)
                {
                    // Use RunContinuationsAsynchronously to prevent calls to StopProcessing from potentially
                    // triggering the side-effects of ProcessCommands asynchronously completing inline
                    // which can _potentially_ cause breaking behavior.
                    taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    StopProcessingFlags.Add(instance, taskCompletionSource);
                }
            }

            return result;
        }
    }
}
