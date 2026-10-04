using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DevBridge.Server;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Trace
{
    /// <summary>
    /// The tracepoints. Each traced method gets one Harmony patch under DevBridge's trace id, so unpatching removes exactly
    /// DevBridge's trace patches and never another mod's. Adding and removing happen on the main thread; a patched call
    /// finds its tracepoint from any thread through a map that is replaced, never changed.
    /// </summary>
    internal static class Tracer
    {
        internal const string HarmonyId = "DevBridge.trace";

        private static readonly Harmony Patcher = new Harmony(HarmonyId);
        private static readonly List<Tracepoint> Points = new List<Tracepoint>();
        private static readonly HashSet<IntPtr> Patched = new HashSet<IntPtr>();
        private static volatile Dictionary<IntPtr, Tracepoint> active = new Dictionary<IntPtr, Tracepoint>();
        private static int nextId = 1;

        /// <summary>Any thread: the switched-on tracepoint of a patched method, or null. Keyed by method handle, since
        /// Harmony's MethodBase for a call need not be the same object as the one that was patched.</summary>
        internal static Tracepoint Active(MethodBase method) =>
            method != null && active.TryGetValue(method.MethodHandle.Value, out Tracepoint point) && point.IsOn ? point : null;

        internal static Tracepoint Add(MethodBase method, TraceOptions options)
        {
            MethodRules.Check(method);
            Tracepoint running = Active(method);
            if (running != null) throw new BridgeException($"{running.Label} is already traced as id {running.Id}: off={running.Id} first");
            TraceRecorder.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            var point = new Tracepoint(nextId++, method, options);
            Publish(point.Key, point);
            if (!Patched.Contains(point.Key)) Patch(point);
            Points.Add(point);
            return point;
        }

        internal static Tracepoint Find(int id) =>
            Points.FirstOrDefault(point => point.Id == id) ?? throw new BridgeException($"no tracepoint {id}: /trace lists them");

        internal static List<Dictionary<string, object>> List() => Points.Select(point => point.Summary()).ToList();

        /// <summary>off=N or off=all: switches tracepoints off and unpatches their methods; their calls stay readable.</summary>
        internal static int Off(string which)
        {
            List<Tracepoint> points = which.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? Points.Where(point => point.IsOn).ToList()
                : new List<Tracepoint> { Find(int.TryParse(which, out int id) ? id : throw new BridgeException("off= takes an id or all")) };
            foreach (Tracepoint point in points)
            {
                point.SwitchOff();
                Retire(point);
            }
            return points.Count;
        }

        /// <summary>Forgets the tracepoints that are off, with their calls.</summary>
        internal static int Forget() => Points.RemoveAll(point => !point.IsOn);

        /// <summary>Main thread: takes a switched-off tracepoint out of the call path, and unpatches its method unless a
        /// newer tracepoint on the same method has taken its place.</summary>
        internal static void Retire(Tracepoint point)
        {
            if (point.IsOn) return;
            if (active.TryGetValue(point.Key, out Tracepoint current) && current == point) Unpublish(point.Key);
            if (active.ContainsKey(point.Key) || !Patched.Remove(point.Key)) return;
            Unpatch(point.Method);
        }

        private static void Patch(Tracepoint point)
        {
            try
            {
                Patcher.Patch(point.Method, prefix: TraceHooks.Prefix(point.Method), postfix: TraceHooks.Postfix(point.Method),
                    finalizer: TraceHooks.Finalizer());
                Patched.Add(point.Key);
            }
            catch (Exception error)
            {
                Unpublish(point.Key);
                // a failed patch stays in Harmony's patch list and would break the next mod that patches the method
                Unpatch(point.Method);
                Exception cause = error.InnerException ?? error;
                throw new BridgeException($"Harmony could not patch {point.Label}: {cause.GetType().Name}: {cause.Message}");
            }
        }

        private static void Unpatch(MethodBase method)
        {
            try
            {
                Patcher.Unpatch(method, HarmonyPatchType.All, HarmonyId);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[DevBridge] trace: unpatching {method.Name} failed, {error.GetType().Name}: {error.Message}");
            }
        }

        /// <summary>Other Harmony ids patching the method, so a reply can say which mods change it.</summary>
        internal static List<string> OtherPatches(MethodBase method) =>
            Harmony.GetPatchInfo(method)?.Owners.Where(owner => owner != HarmonyId).ToList() ?? new List<string>();

        private static void Publish(IntPtr key, Tracepoint point) =>
            active = new Dictionary<IntPtr, Tracepoint>(active) { [key] = point };

        private static void Unpublish(IntPtr key)
        {
            var map = new Dictionary<IntPtr, Tracepoint>(active);
            map.Remove(key);
            active = map;
        }
    }
}
