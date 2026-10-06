using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Logs;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Profiling;

namespace DevBridge.Heap
{
    /// <summary>
    /// /heap?seconds=: every frame's time, the managed heap in use and the collection count, so each hitch can be put
    /// beside the collections around it. Nothing is patched: it costs two reads a frame. Between collections the heap
    /// in use only grows (Boehm frees at a collection), so its growth is a lower bound on the allocation rate: space
    /// reused inside partly filled blocks after a collection is not seen. Each hitch also carries the log lines written
    /// from the frame before it to the frame after, and how many networked objects appeared or went in its frame.
    /// </summary>
    internal sealed class HeapWatch
    {
        private sealed class Collection
        {
            internal float At;
            internal int Frame;
            internal float Ms;
            internal float NextMs = -1f;
            internal long Before;
            internal long After;
        }

        private sealed class Hitch
        {
            internal float At;
            internal int Frame;
            internal float Ms;
            internal int Objects;
            internal long FromLine;
            internal long ToLine = -1;
        }

        private readonly float seconds;
        private readonly float hitchMs;
        private readonly int max;
        private readonly List<float> frames = new List<float>(8192);
        private readonly List<Collection> collections = new List<Collection>();
        private readonly List<Hitch> hitches = new List<Hitch>();
        private Collection pending;
        private Hitch pendingHitch;
        private long olderLine;
        private long lastLine;
        private int lastObjects;
        private float start;
        private long usedStart;
        private long lastUsed;
        private long grown;
        private int lastCount;

        private HeapWatch(float seconds, float hitchMs, int max)
        {
            this.seconds = seconds;
            this.hitchMs = hitchMs;
            this.max = max;
        }

        internal static void Start(BridgeRequest request)
        {
            var watch = new HeapWatch(Mathf.Clamp(request.Float("seconds", 30f), 0.5f, 120f),
                Mathf.Max(1f, request.Float("hitch", 40f)), Mathf.Clamp(request.Int("max", 50), 1, 1000));
            Async.Start(request, watch.Run(request));
        }

        private IEnumerator Run(BridgeRequest request)
        {
            start = Time.realtimeSinceStartup;
            usedStart = lastUsed = Profiler.GetMonoUsedSizeLong();
            lastCount = GC.CollectionCount(0);
            olderLine = lastLine = LogCapture.Lines.Next;
            lastObjects = NetworkedObjects();
            float end = start + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                Frame();
            }
            request.Json(Reply());
        }

        private void Frame()
        {
            float ms = Time.unscaledDeltaTime * 1000f, at = Time.realtimeSinceStartup - start;
            long used = Profiler.GetMonoUsedSizeLong();
            int count = GC.CollectionCount(0);
            frames.Add(ms);
            if (pending != null) pending.NextMs = ms;
            pending = null;
            if (count != lastCount)
                collections.Add(pending = new Collection { At = at, Frame = Time.frameCount, Ms = ms, Before = lastUsed, After = used });
            else if (used > lastUsed) grown += used - lastUsed;
            NoteHitch(ms, at);
            lastUsed = used;
            lastCount = count;
        }

        /// <summary>A frame over hitch= ms, with the log from the read before the frame's start to the next frame's read.</summary>
        private void NoteHitch(float ms, float at)
        {
            long line = LogCapture.Lines.Next;
            int objects = NetworkedObjects();
            if (pendingHitch != null) pendingHitch.ToLine = line;
            pendingHitch = null;
            if (ms >= hitchMs)
                hitches.Add(pendingHitch = new Hitch { At = at, Frame = Time.frameCount, Ms = ms, Objects = objects - lastObjects, FromLine = olderLine });
            olderLine = lastLine;
            lastLine = line;
            lastObjects = objects;
        }

        private static int NetworkedObjects() => ZNetScene.instance != null ? ZNetScene.instance.m_instances.Count : 0;

        private static List<string> LogAround(Hitch hitch)
        {
            long to = hitch.ToLine >= 0 ? hitch.ToLine : LogCapture.Lines.Next;
            return LogCapture.Lines.Since(hitch.FromLine, 8, line => line.Seq < to).Select(line => Fmt.Clip(line.Text, 200)).ToList();
        }

        private Dictionary<string, object> Reply()
        {
            float elapsed = Mathf.Max(Time.realtimeSinceStartup - start, 1e-3f);
            int near = hitches.Count(h => NearCollection(h.Frame));
            return new Dictionary<string, object>
            {
                ["seconds"] = Fmt.R(elapsed),
                ["frames"] = FramesPart(elapsed),
                ["heap"] = HeapPart(elapsed),
                ["collections"] = CollectionsPart(elapsed),
                ["hitches"] = HitchesPart(near),
                ["verdict"] = Verdict(near),
            };
        }

        /// <summary>A frame's time is read the frame after it ran, so a collection one frame either side counts.</summary>
        private bool NearCollection(int frame) => collections.Any(c => Math.Abs(c.Frame - frame) <= 1);

        private Dictionary<string, object> FramesPart(float elapsed)
        {
            List<float> sorted = frames.OrderBy(f => f).ToList();
            return new Dictionary<string, object>
            {
                ["count"] = frames.Count,
                ["fps"] = Fmt.R(frames.Count / elapsed),
                ["p99_ms"] = sorted.Count == 0 ? 0f : Fmt.R(sorted[Mathf.Clamp(Mathf.CeilToInt(0.99f * sorted.Count) - 1, 0, sorted.Count - 1)]),
                ["max_ms"] = sorted.Count == 0 ? 0f : Fmt.R(sorted[sorted.Count - 1]),
            };
        }

        private Dictionary<string, object> HeapPart(float elapsed) => new Dictionary<string, object>
        {
            ["used_start_mb"] = HeapNow.Mb(usedStart),
            ["used_end_mb"] = HeapNow.Mb(lastUsed),
            ["heap_mb"] = HeapNow.Mb(Profiler.GetMonoHeapSizeLong()),
            ["growth_mb_per_s"] = Fmt.R(grown / 1048576f / elapsed),
            ["growth_note"] = "the heap in use growing between collections: a lower bound on the managed allocation rate",
        };

        private Dictionary<string, object> CollectionsPart(float elapsed) => new Dictionary<string, object>
        {
            ["count"] = collections.Count,
            ["every_seconds"] = collections.Count == 0 ? (object)null : Fmt.R(elapsed / collections.Count),
            ["avg_freed_mb"] = collections.Count == 0 ? 0f : HeapNow.Mb((long)collections.Average(c => c.Before - c.After)),
            ["list"] = collections.Take(max).Select(c => new Dictionary<string, object>
            {
                ["at"] = Fmt.R(c.At),
                ["frame"] = c.Frame,
                ["frame_ms"] = new[] { Fmt.R(c.Ms), Fmt.R(c.NextMs) },
                ["used_before_mb"] = HeapNow.Mb(c.Before),
                ["used_after_mb"] = HeapNow.Mb(c.After),
            }).ToList(),
        };

        private Dictionary<string, object> HitchesPart(int near) => new Dictionary<string, object>
        {
            ["over_ms"] = hitchMs,
            ["count"] = hitches.Count,
            ["near_collection"] = near,
            ["list"] = hitches.OrderByDescending(h => h.Ms).Take(max).Select(h => new Dictionary<string, object>
            {
                ["at"] = Fmt.R(h.At),
                ["frame"] = h.Frame,
                ["ms"] = Fmt.R(h.Ms),
                ["collection"] = NearCollection(h.Frame),
                ["objects_change"] = h.Objects,
                ["log"] = LogAround(h),
            }).ToList(),
        };

        private string Verdict(int near)
        {
            if (hitches.Count == 0) return $"no frame over {hitchMs} ms";
            string share = $"{near} of {hitches.Count} frames over {hitchMs} ms came within a frame of a garbage collection";
            if (near == hitches.Count) return share + ": the hitches are the collector";
            return near == 0 ? share + ": the hitches are not the collector" : share;
        }
    }
}
