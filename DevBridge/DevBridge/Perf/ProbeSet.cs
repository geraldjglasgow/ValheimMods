using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DevBridge.Perf
{
    /// <summary>
    /// Puts the probes on the targets and takes exactly those off again, all under one Harmony id and a budget of
    /// milliseconds per frame, so a long list hitches the game a little over many frames rather than once for seconds.
    /// A failure on one method is noted and skipped. RemoveAll is the safety net: everything left, at once.
    /// </summary>
    internal sealed class ProbeSet
    {
        internal const string HarmonyId = "DevBridge.perf";

        private readonly Harmony harmony = new Harmony(HarmonyId);
        private readonly Queue<Action> work = new Queue<Action>();
        private readonly List<MethodBase> probed = new List<MethodBase>();
        private readonly Stopwatch busy = new Stopwatch();

        internal readonly List<string> Failed = new List<string>();
        internal int Rebuilt;

        internal Harmony Harmony => harmony;
        internal int Probed => probed.Count;
        internal double BusySeconds => busy.Elapsed.TotalSeconds;

        /// <summary>Queues a probe on every target, then a rebuild of each wrapper that may have inlined a small one.</summary>
        internal void Plan(IEnumerable<PerfTarget> targets)
        {
            List<PerfTarget> list = targets.ToList();
            foreach (PerfTarget target in list) work.Enqueue(() => Probe(target));
            foreach (MethodBase original in list.Where(t => t.MayBeInlined).SelectMany(t => t.On).Distinct())
                work.Enqueue(() => Rebuild(original));
        }

        /// <summary>Runs queued work for about budgetMs; true while more is left.</summary>
        internal bool Step(double budgetMs)
        {
            busy.Start();
            var frame = Stopwatch.StartNew();
            while (work.Count > 0 && frame.Elapsed.TotalMilliseconds < budgetMs) work.Dequeue()();
            busy.Stop();
            return work.Count > 0;
        }

        /// <summary>Drops whatever was still queued and queues the removal of every probe put on.</summary>
        internal void PlanRemoval()
        {
            work.Clear();
            foreach (MethodBase method in probed.ToList()) work.Enqueue(() => Unprobe(method));
        }

        /// <summary>Takes off every probe still on, now, then sweeps for any of this id's patches left anywhere.</summary>
        internal void RemoveAll()
        {
            work.Clear();
            foreach (MethodBase method in probed.ToList()) Unprobe(method);
            Safely("sweep", () => Harmony.UnpatchID(HarmonyId));
        }

        private void Probe(PerfTarget target)
        {
            var (prefix, postfix) = ProbeEmitter.For(target);
            if (Safely(Name(target.Method), () => harmony.Patch(target.Method, prefix, postfix))) probed.Add(target.Method);
            else Safely("clean " + Name(target.Method), () => harmony.Unpatch(target.Method, HarmonyPatchType.All, HarmonyId));
        }

        /// <summary>Patching with nothing new makes Harmony build the wrapper again, which now calls the probed method.</summary>
        private void Rebuild(MethodBase original)
        {
            if (Safely("rebuild " + Name(original), () => harmony.Patch(original))) Rebuilt++;
        }

        private void Unprobe(MethodBase method)
        {
            probed.Remove(method);
            Safely("remove " + Name(method), () => harmony.Unpatch(method, HarmonyPatchType.All, HarmonyId));
        }

        private bool Safely(string what, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception error)
            {
                Failed.Add($"{what}: {error.GetType().Name}: {Clip(error.Message)}");
                return false;
            }
        }

        private static string Clip(string text) => text.Length <= 200 ? text : text.Substring(0, 200) + "...";

        internal static string Name(MethodBase method) => $"{method.DeclaringType?.FullName?.Replace('+', '.')}.{method.Name}";
    }
}
