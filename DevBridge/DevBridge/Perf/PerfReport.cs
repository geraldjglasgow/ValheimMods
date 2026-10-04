using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevBridge.Perf
{
    /// <summary>
    /// The timed part of a /perf reply: per mod its main-thread milliseconds and calls per frame and its costliest
    /// methods, the transpilers it could not time, and what the timing itself cost.
    /// </summary>
    internal static class PerfReport
    {
        private const string Note =
            "ms_per_frame is main-thread time inside timed methods, counted once per outermost call of the mod, so its own " +
            "timed methods nested in each other are not added twice; time in another mod's timed method inside it counts " +
            "for both, and so does mods_ms_per_frame. Each timed call costs extra_us_per_call more than untimed (compare " +
            "frames with baseline=1) and reads at least empty_reads_us; timing_cost_ms is the most of a mod's figure that " +
            "could be the timing itself. Calls that throw are not counted; calls on other threads are listed apart.";

        internal static void Add(Dictionary<string, object> reply, PerfSession session)
        {
            int frames = Math.Max(session.Frames.Count, 1);
            double extra = session.Cost?.ExtraMicroseconds ?? 0;
            List<IGrouping<PerfMod, PerfTarget>> mods = session.Plan.Targets.GroupBy(t => t.Mod).ToList();
            reply["mods_ms_per_frame"] = Ms(mods.Sum(g => g.Key.Ticks), frames);
            reply["mods"] = mods.Where(Called).OrderByDescending(g => g.Key.Ticks)
                .Select(g => Mod(g.Key, g.ToList(), frames, session.Options.Top, extra)).ToList();
            reply["idle"] = mods.Where(g => !Called(g)).Select(g => $"{g.Key.Name} ({g.Count()} methods)").OrderBy(n => n).ToList();
            reply["transpilers"] = Transpilers(session.Plan);
            reply["timing"] = Timing(session);
        }

        private static bool Called(IGrouping<PerfMod, PerfTarget> mod) => mod.Any(t => t.Counter.Calls + t.Counter.OtherCalls > 0);

        private static Dictionary<string, object> Mod(PerfMod mod, List<PerfTarget> targets, int frames, int top, double extra)
        {
            long calls = targets.Sum(t => t.Counter.Calls);
            var entry = new Dictionary<string, object>
            {
                ["mod"] = mod.Name,
                ["ms_per_frame"] = Ms(mod.Ticks, frames),
                ["calls_per_frame"] = Fmt.R((float)calls / frames),
                ["timing_cost_ms"] = Round3(calls * extra / 1000 / frames),
                ["methods"] = targets.Count,
                ["top"] = targets.Where(t => t.Counter.Calls > 0).OrderByDescending(t => t.Counter.Ticks).Take(top).Select(t => Entry(t, frames)).ToList(),
            };
            long other = targets.Sum(t => t.Counter.OtherCalls);
            if (other > 0) entry["other_threads"] = new Dictionary<string, object> { ["calls"] = other, ["ms"] = Ms(targets.Sum(t => t.Counter.OtherTicks), 1) };
            return entry;
        }

        private static Dictionary<string, object> Entry(PerfTarget target, int frames)
        {
            PerfCounter counter = target.Counter;
            var entry = new Dictionary<string, object>
            {
                ["method"] = ProbeSet.Name(target.Method),
                ["kind"] = string.Join("+", target.Kinds),
                ["ms_per_frame"] = Ms(counter.Ticks, frames),
                ["calls_per_frame"] = Fmt.R((float)counter.Calls / frames),
                ["us_per_call"] = Fmt.R((float)(Calibration.Micro(counter.Ticks) / counter.Calls)),
                ["max_ms"] = Ms(counter.Max, 1),
            };
            if (target.On.Count > 0) entry["on"] = On(target.On);
            return entry;
        }

        private static List<string> On(List<MethodBase> on) => Few(on.Select(PerfPlan.Short).ToList(), 3);

        private static List<string> Few(List<string> names, int keep)
        {
            if (names.Count <= keep) return names.ToList();
            return names.Take(keep).Concat(new[] { $"and {names.Count - keep} more" }).ToList();
        }

        private static List<Dictionary<string, object>> Transpilers(PerfPlan plan) => plan.Transpilers.OrderBy(t => t.Key.Name)
            .Select(t => new Dictionary<string, object>
            {
                ["mod"] = t.Key.Name,
                ["count"] = t.Value.Count,
                ["on"] = Few(t.Value.Distinct().ToList(), 12),
            }).ToList();

        private static Dictionary<string, object> Timing(PerfSession session) => new Dictionary<string, object>
        {
            ["methods"] = session.Timed,
            ["wrappers_rebuilt"] = session.Probes.Rebuilt,
            ["untimeable"] = Few(session.Plan.Untimeable, 10),
            ["patch_seconds"] = Fmt.R((float)session.ApplySeconds),
            ["extra_us_per_call"] = session.Cost == null ? null : (object)Round3(session.Cost.ExtraMicroseconds),
            ["empty_reads_us"] = session.Cost == null ? null : (object)Round3(session.Cost.FloorMicroseconds),
            ["calibration"] = session.CostError,
            ["failed"] = Few(session.Probes.Failed, 20),
            ["probe_faults"] = PerfProbe.Faults == 0 ? null : $"{PerfProbe.Faults}, first: {PerfProbe.Fault}",
            ["note"] = Note,
        };

        /// <summary>The event /perf publishes when a sample is over: frames and the five costliest mods.</summary>
        internal static Dictionary<string, object> Summary(Dictionary<string, object> reply, PerfSession session)
        {
            var summary = new Dictionary<string, object> { ["seconds"] = session.Options.Seconds, ["frames"] = reply["frames"] };
            if (session.Options.Mod != null) summary["mod"] = session.Options.Mod;
            if (reply.TryGetValue("mods", out object mods))
                summary["mods"] = ((List<Dictionary<string, object>>)mods).Take(5).Select(m => $"{m["mod"]} {m["ms_per_frame"]} ms").ToList();
            if (session.Probes.Failed.Count > 0) summary["failed"] = session.Probes.Failed.Count;
            return summary;
        }

        private static double Ms(long ticks, int frames) => Round3(Calibration.Micro(ticks) / 1000 / frames);

        /// <summary>Three decimals, not Fmt.R's two: a mod's cost per frame is often a few microseconds.</summary>
        private static double Round3(double value) => Math.Round(value, 3);
    }
}
