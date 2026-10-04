using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;

namespace DevBridge.Perf
{
    /// <summary>
    /// Managed allocation over a sample, from the first source that really counts it here: Unity's "GC Allocated In
    /// Frame" recorder (development builds), else the main thread's allocation count (GC.GetAllocatedBytesForCurrentThread
    /// is declared in this Mono but reads 0 under its Boehm collector, so it is tried on a known allocation first).
    /// Otherwise it says so: the heap's size is not an allocation rate, since freed space is reused without growing it.
    /// </summary>
    internal sealed class AllocationMeter
    {
        private static Func<long> threadCounter;
        private static bool threadChecked;
        private static byte[] probe;

        private ProfilerRecorder recorder;
        private long recorded;
        private long threadStart;
        private long threadEnd;

        internal void Begin()
        {
            recorder = Recorder();
            recorded = 0;
            threadStart = ThreadCounter()?.Invoke() ?? 0;
        }

        internal void Frame()
        {
            if (recorder.Valid) recorded += recorder.LastValue;
        }

        internal void End()
        {
            threadEnd = ThreadCounter()?.Invoke() ?? 0;
            recorder.Dispose();
        }

        internal object Report(int frames)
        {
            frames = Math.Max(frames, 1);
            if (recorded > 0) return Rate(recorded, frames, "Unity's GC Allocated In Frame recorder");
            if (threadCounter != null) return Rate(threadEnd - threadStart, frames, "the main thread's allocation count");
            return "not measurable in this build: Unity's allocation recorder is development-only and this Mono's collector keeps no count";
        }

        private static Dictionary<string, object> Rate(long bytes, int frames, string source) => new Dictionary<string, object>
        {
            ["kb_per_frame"] = Fmt.R(bytes / 1024f / frames),
            ["mb_total"] = Fmt.R(bytes / 1048576f),
            ["source"] = source,
        };

        private static ProfilerRecorder Recorder()
        {
            try
            {
                return ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>The per-thread count, kept only if it sees a 256 KB allocation made just for this check.</summary>
        private static Func<long> ThreadCounter()
        {
            if (threadChecked) return threadCounter;
            threadChecked = true;
            MethodInfo method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (method == null || method.ReturnType != typeof(long)) return null;
            var candidate = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            long before = candidate();
            probe = new byte[256 * 1024];
            if (candidate() - before >= probe.Length) threadCounter = candidate;
            probe = null;
            return threadCounter;
        }
    }
}
