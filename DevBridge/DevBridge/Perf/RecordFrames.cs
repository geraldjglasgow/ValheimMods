using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace DevBridge.Perf
{
    /// <summary>
    /// What a recording writes each frame: one fixed record of the frame (FrameFields) and one entry (EntryFields) for each
    /// timed method that ran in it on the main thread, each that ran on other threads, and each mod's outermost time; the
    /// counters are cleared after. A frame is step to step on the main thread, so its milliseconds hold exactly the calls
    /// counted in it. Nothing here allocates.
    /// </summary>
    internal sealed class RecordFrames
    {
        internal const int FrameBytes = 56;
        internal const int EntryBytes = 20;

        /// <summary>The frame record, in the order written: name and numpy type (little-endian).</summary>
        internal static readonly string[][] FrameFields =
        {
            new[] { "seq", "u4" }, new[] { "frame", "u4" }, new[] { "t", "f8" }, new[] { "ms", "f4" }, new[] { "unity_ms", "f4" },
            new[] { "gc", "u2" }, new[] { "flags", "u2" }, new[] { "entries", "u4" }, new[] { "mono_kb", "u4" },
            new[] { "instances", "u4" }, new[] { "peers", "u2" }, new[] { "spare", "u2" },
            new[] { "x", "f4" }, new[] { "y", "f4" }, new[] { "z", "f4" },
        };

        internal static readonly string[][] EntryFields =
        {
            new[] { "seq", "u4" }, new[] { "id", "u2" }, new[] { "kind", "u1" }, new[] { "spare", "u1" },
            new[] { "ticks", "u4" }, new[] { "calls", "u4" }, new[] { "max", "u4" },
        };

        private struct Tally
        {
            internal uint Count;
            internal bool Dropped;
        }

        private readonly PerfTarget[] targets;
        private readonly PerfMod[] mods;
        private readonly long minTicks;
        private long start;
        private long last;
        private int collections;
        private uint seq;

        internal readonly long[] ModTicks;
        internal int Frames;
        internal int Dropped;
        internal int Over50;
        internal double Seconds;
        internal double MaxMs;

        internal RecordFrames(PerfPlan plan, double minMicros)
        {
            targets = plan.Targets.ToArray();
            mods = plan.Mods.All.ToArray();
            ModTicks = new long[mods.Length];
            minTicks = (long)(minMicros * Stopwatch.Frequency / 1e6);
        }

        /// <summary>Zeroes what probing and calibration counted, so the first frame holds only its own calls.</summary>
        internal void Begin()
        {
            foreach (PerfTarget target in targets) target.Counter.Clear();
            foreach (PerfMod mod in mods) mod.Ticks = 0;
            PerfProbe.SettleMainThread();
            start = last = Stopwatch.GetTimestamp();
            collections = GC.CollectionCount(0);
        }

        internal void Write(RecordStream frames, RecordStream entries)
        {
            long now = Stopwatch.GetTimestamp();
            PerfProbe.SettleMainThread();
            var tally = new Tally();
            for (int i = 0; i < targets.Length; i++) Target(i, targets[i].Counter, entries, ref tally);
            foreach (PerfMod mod in mods) Mod(mod, entries, ref tally);
            Frame(now, tally, frames);
            last = now;
            seq++;
        }

        private void Target(int id, PerfCounter counter, RecordStream entries, ref Tally tally)
        {
            if (counter.Calls > 0)
            {
                if (counter.Ticks >= minTicks) Entry(entries, id, 0, counter.Ticks, counter.Calls, counter.Max, ref tally);
                counter.Ticks = counter.Calls = counter.Max = 0;
            }
            if (counter.OtherCalls == 0) return;
            long calls = Interlocked.Exchange(ref counter.OtherCalls, 0);
            long ticks = Interlocked.Exchange(ref counter.OtherTicks, 0);
            if (ticks >= minTicks) Entry(entries, id, 1, ticks, calls, 0, ref tally);
        }

        private void Mod(PerfMod mod, RecordStream entries, ref Tally tally)
        {
            if (mod.Ticks == 0) return;
            ModTicks[mod.Index] += mod.Ticks;
            Entry(entries, mod.Index, 2, mod.Ticks, 0, 0, ref tally);
            mod.Ticks = 0;
        }

        private void Entry(RecordStream entries, int id, byte kind, long ticks, long calls, long max, ref Tally tally)
        {
            if (!entries.Room(EntryBytes))
            {
                tally.Dropped = true;
                return;
            }
            entries.U4(seq);
            entries.U2(id);
            entries.U1(kind);
            entries.U1(0);
            entries.U4(ticks);
            entries.U4(calls);
            entries.U4(max);
            tally.Count++;
        }

        private void Frame(long now, Tally tally, RecordStream frames)
        {
            float ms = (float)Ms(now - last);
            Count(ms, tally.Dropped);
            if (!frames.Room(FrameBytes))
            {
                if (!tally.Dropped) Dropped++;
                return;
            }
            frames.U4(seq);
            frames.U4((uint)Time.frameCount);
            frames.F8((now - start) / (double)Stopwatch.Frequency);
            frames.F4(ms);
            frames.F4(Time.unscaledDeltaTime * 1000f);
            frames.U2(Collections());
            frames.U2(tally.Dropped ? 1 : 0);
            frames.U4(tally.Count);
            World(frames);
        }

        private void World(RecordStream frames)
        {
            frames.U4(Profiler.GetMonoUsedSizeLong() / 1024);
            frames.U4(ZNetScene.instance != null ? ZNetScene.instance.m_instances.Count : 0);
            frames.U2(ZNet.instance != null ? Math.Min(ZNet.instance.m_peers.Count, ushort.MaxValue) : 0);
            frames.U2(0);
            Vector3 at = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : new Vector3(float.NaN, float.NaN, float.NaN);
            frames.F4(at.x);
            frames.F4(at.y);
            frames.F4(at.z);
        }

        private void Count(float ms, bool dropped)
        {
            Frames++;
            Seconds += ms / 1000.0;
            if (ms > MaxMs) MaxMs = ms;
            if (ms > 50) Over50++;
            if (dropped) Dropped++;
        }

        private int Collections()
        {
            int now = GC.CollectionCount(0);
            int happened = now - collections;
            collections = now;
            return Math.Min(happened, ushort.MaxValue);
        }

        internal static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }
}
