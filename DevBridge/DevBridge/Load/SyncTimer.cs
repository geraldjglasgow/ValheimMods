using System.Diagnostics;
using HarmonyLib;

namespace DevBridge.Load
{
    /// <summary>
    /// Times ZDOMan.Update, the game's world sync once a frame from ZNet.Update (the ownership pass every two seconds on
    /// the server, the send loop, destroyed objects), with a stopwatch prefix that runs before every other prefix and a
    /// postfix that runs after every other postfix, so other mods' patches on it (HighSeat's) count too. On only while a
    /// /load measure runs, under its own Harmony id; each frame's time is taken by the sample after the frame's updates.
    /// </summary>
    internal static class SyncTimer
    {
        private const string HarmonyId = DevBridgePlugin.PluginGuid + ".load.sync";
        private static Harmony harmony;
        private static long started;
        private static long frameTicks;

        internal static void Start()
        {
            if (harmony != null) return;
            frameTicks = 0;
            started = 0;
            harmony = new Harmony(HarmonyId);
            var before = new HarmonyMethod(typeof(SyncTimer), nameof(Before)) { priority = Priority.First };
            var after = new HarmonyMethod(typeof(SyncTimer), nameof(After)) { priority = Priority.Last };
            harmony.Patch(AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.Update)), prefix: before, postfix: after);
        }

        internal static void Stop()
        {
            if (harmony == null) return;
            harmony.UnpatchSelf();
            harmony = null;
        }

        /// <summary>Milliseconds ZDOMan.Update took since the last call (this frame's), and the count starts again.</summary>
        internal static float TakeFrameMs()
        {
            float ms = (float)(frameTicks * 1000.0 / Stopwatch.Frequency);
            frameTicks = 0;
            return ms;
        }

        private static void Before() => started = Stopwatch.GetTimestamp();

        private static void After()
        {
            if (started == 0) return;
            frameTicks += Stopwatch.GetTimestamp() - started;
            started = 0;
        }
    }
}
