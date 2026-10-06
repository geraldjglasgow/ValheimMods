using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace DevBridge.Heap
{
    /// <summary>
    /// The memory figures at this moment: the managed (Mono) heap and its collector, and Unity's native allocator (the
    /// process's own figures read 0 under Unity's Mono, so they are left out).
    /// </summary>
    internal static class HeapNow
    {
        internal static Dictionary<string, object> Snapshot() => new Dictionary<string, object>
        {
            ["managed"] = Managed(),
            ["gc"] = Collector(),
            ["native"] = Native(),
        };

        private static Dictionary<string, object> Managed()
        {
            long used = Profiler.GetMonoUsedSizeLong(), heap = Profiler.GetMonoHeapSizeLong();
            return new Dictionary<string, object>
            {
                ["used_mb"] = Mb(used),
                ["heap_mb"] = Mb(heap),
                ["free_mb"] = Mb(heap - used),
                ["gc_total_mb"] = Mb(GC.GetTotalMemory(false)),
            };
        }

        private static Dictionary<string, object> Collector() => new Dictionary<string, object>
        {
            ["collections"] = Enumerable.Range(0, GC.MaxGeneration + 1).ToDictionary(g => "gen" + g, g => (object)GC.CollectionCount(g)),
            ["mode"] = GarbageCollector.GCMode.ToString(),
            ["incremental"] = GarbageCollector.isIncremental,
            ["slice_ms"] = Fmt.R(GarbageCollector.incrementalTimeSliceNanoseconds / 1e6f),
        };

        private static Dictionary<string, object> Native() => new Dictionary<string, object>
        {
            ["allocated_mb"] = Mb(Profiler.GetTotalAllocatedMemoryLong()),
            ["reserved_mb"] = Mb(Profiler.GetTotalReservedMemoryLong()),
            ["unused_reserved_mb"] = Mb(Profiler.GetTotalUnusedReservedMemoryLong()),
        };

        internal static float Mb(long bytes) => Fmt.R(bytes / 1048576f);
    }
}
