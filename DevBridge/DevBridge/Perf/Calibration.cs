using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace DevBridge.Perf
{
    /// <summary>
    /// What timing itself costs on this machine, measured on an empty method called many times untimed, then timed:
    /// the extra time a timed call takes (which slows the frame), and what an empty timed method reads as (which every
    /// timed figure includes).
    /// </summary>
    internal static class Calibration
    {
        private const int Calls = 20000;

        internal sealed class Result
        {
            internal double ExtraMicroseconds;
            internal double FloorMicroseconds;
        }

        /// <summary>Probes the empty method under the sample's Harmony id and takes the probe off again before returning.</summary>
        internal static Result Measure(Harmony harmony, PerfMod mod)
        {
            MethodInfo empty = AccessTools.Method(typeof(Calibration), nameof(Empty));
            var target = new PerfTarget(empty, mod);
            Loop();
            double plain = Loop();
            var (prefix, postfix) = ProbeEmitter.For(target);
            harmony.Patch(empty, prefix, postfix);
            try
            {
                Loop();
                target.Counter.Clear();
                double timed = Loop();
                return new Result { ExtraMicroseconds = (timed - plain) / Calls, FloorMicroseconds = Micro(target.Counter.Ticks) / Calls };
            }
            finally
            {
                harmony.Unpatch(empty, HarmonyPatchType.All, harmony.Id);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Empty()
        {
        }

        /// <summary>Microseconds for the whole loop of calls.</summary>
        private static double Loop()
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < Calls; i++) Empty();
            return Micro(Stopwatch.GetTimestamp() - start);
        }

        internal static double Micro(long ticks) => ticks * 1e6 / Stopwatch.Frequency;
    }
}
