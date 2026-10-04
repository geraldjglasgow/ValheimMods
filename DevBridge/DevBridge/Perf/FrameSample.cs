using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace DevBridge.Perf
{
    /// <summary>
    /// Frame times (Unity's unscaled frame time, so a paused or slowed game still reads true), garbage collections per
    /// generation (Boehm has one, gen0) and the managed heap over one sample.
    /// </summary>
    internal sealed class FrameSample
    {
        private readonly List<float> deltas = new List<float>(4096);
        private readonly AllocationMeter allocations = new AllocationMeter();
        private int[] collections;
        private long usedStart;
        private long usedEnd;

        internal int Count => deltas.Count;

        internal void Begin()
        {
            deltas.Clear();
            collections = Collections();
            usedStart = Profiler.GetMonoUsedSizeLong();
            allocations.Begin();
        }

        internal void Frame()
        {
            deltas.Add(Time.unscaledDeltaTime);
            allocations.Frame();
        }

        internal void End()
        {
            usedEnd = Profiler.GetMonoUsedSizeLong();
            int[] now = Collections();
            for (int generation = 0; generation < now.Length; generation++) collections[generation] = now[generation] - collections[generation];
            allocations.End();
        }

        internal Dictionary<string, object> Frames()
        {
            if (deltas.Count == 0) return new Dictionary<string, object> { ["count"] = 0 };
            List<float> sorted = deltas.OrderBy(d => d).ToList();
            float total = deltas.Sum();
            return new Dictionary<string, object>
            {
                ["count"] = deltas.Count,
                ["seconds"] = Fmt.R(total),
                ["fps"] = Fmt.R(deltas.Count / Mathf.Max(total, 1e-4f)),
                ["ms"] = new Dictionary<string, object>
                {
                    ["avg"] = Fmt.R(total * 1000f / deltas.Count),
                    ["p50"] = Percentile(sorted, 0.50f),
                    ["p95"] = Percentile(sorted, 0.95f),
                    ["p99"] = Percentile(sorted, 0.99f),
                    ["max"] = Fmt.R(sorted[sorted.Count - 1] * 1000f),
                },
            };
        }

        internal Dictionary<string, object> Gc() => new Dictionary<string, object>
        {
            ["collections"] = Enumerable.Range(0, collections.Length).ToDictionary(g => "gen" + g, g => (object)collections[g]),
            ["incremental"] = GarbageCollector.isIncremental,
        };

        internal Dictionary<string, object> Memory() => new Dictionary<string, object>
        {
            ["mono_used_mb"] = new[] { Mb(usedStart), Mb(usedEnd) },
            ["mono_heap_mb"] = Mb(Profiler.GetMonoHeapSizeLong()),
            ["allocated"] = allocations.Report(deltas.Count),
        };

        /// <summary>Nearest-rank percentile, in milliseconds.</summary>
        private static float Percentile(List<float> sorted, float q) =>
            Fmt.R(sorted[Mathf.Clamp(Mathf.CeilToInt(q * sorted.Count) - 1, 0, sorted.Count - 1)] * 1000f);

        private static int[] Collections() => Enumerable.Range(0, GC.MaxGeneration + 1).Select(GC.CollectionCount).ToArray();

        private static float Mb(long bytes) => Fmt.R(bytes / 1048576f);
    }
}
