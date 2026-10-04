using System;
using System.Diagnostics;

namespace DevBridge.Perf
{
    /// <summary>What a probe's prefix hands its postfix through Harmony's __state: the counter, the thread, the start.</summary>
    public struct PerfMark
    {
        internal PerfCounter Counter;
        internal PerfLane Lane;
        internal long Start;
        internal bool Outer;
    }

    /// <summary>One thread's view of a sample: whether it is the main thread, and how deep each mod's timed calls are on it.</summary>
    internal sealed class PerfLane
    {
        internal readonly bool Main;
        internal readonly int[] Depth;

        internal PerfLane(bool main, int mods)
        {
            Main = main;
            Depth = new int[mods];
        }
    }

    /// <summary>
    /// The hot path every timed call passes through, from the probe classes ProbeEmitter makes. Public only because
    /// those classes live in their own run-time assembly; nothing else calls it. A failure here is caught and kept for
    /// the reply (never thrown into the game, never logged per call).
    /// </summary>
    public static class PerfProbe
    {
        [ThreadStatic] private static PerfLane lane;
        private static PerfLane mainLane;

        internal static int MainThread;
        internal static int ModCount;
        internal static string Fault;
        internal static int Faults;

        public static void Enter(PerfCounter counter, out PerfMark mark)
        {
            mark = default;
            try
            {
                Begin(counter, ref mark);
            }
            catch (Exception error)
            {
                Failed(error);
            }
        }

        public static void Leave(PerfMark mark)
        {
            long ticks = Stopwatch.GetTimestamp() - mark.Start;
            try
            {
                End(mark, ticks);
            }
            catch (Exception error)
            {
                Failed(error);
            }
        }

        private static void Begin(PerfCounter counter, ref PerfMark mark)
        {
            PerfLane current = lane;
            if (current == null || current.Depth.Length < ModCount) current = NewLane();
            mark.Counter = counter;
            mark.Lane = current;
            mark.Outer = current.Depth[counter.Mod.Index]++ == 0;
            mark.Start = Stopwatch.GetTimestamp();
        }

        private static void End(PerfMark mark, long ticks)
        {
            PerfCounter counter = mark.Counter;
            if (counter == null) return;
            int[] depth = mark.Lane.Depth;
            if (--depth[counter.Mod.Index] < 0) depth[counter.Mod.Index] = 0;
            if (mark.Lane.Main) counter.OnMain(ticks, mark.Outer);
            else counter.OnOther(ticks);
        }

        private static PerfLane NewLane()
        {
            bool main = System.Threading.Thread.CurrentThread.ManagedThreadId == MainThread;
            lane = new PerfLane(main, Math.Max(ModCount, 1));
            if (main) mainLane = lane;
            return lane;
        }

        /// <summary>
        /// Called on the main thread between frames, when no timed call can be running there: a timed method that threw
        /// skipped its postfix and left its mod one level deep, which would hide that mod's later calls from its total.
        /// </summary>
        internal static void SettleMainThread()
        {
            PerfLane main = mainLane;
            if (main != null) Array.Clear(main.Depth, 0, main.Depth.Length);
        }

        internal static void Reset(int mainThread, int mods)
        {
            MainThread = mainThread;
            ModCount = mods;
            Fault = null;
            Faults = 0;
            SettleMainThread();
        }

        private static void Failed(Exception error)
        {
            Faults++;
            if (Fault == null) Fault = error.GetType().Name + ": " + error.Message;
        }
    }
}
