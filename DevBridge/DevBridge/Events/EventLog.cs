using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>One thing that happened in the game, numbered so a caller can ask for everything after the last it saw.
    /// It holds plain data only (strings, numbers, lists, maps), so any thread may read and serialise it.</summary>
    internal sealed class GameEvent
    {
        internal long Seq;
        internal string Kind;
        internal DateTime Clock;
        internal float? GameTime;
        internal Dictionary<string, object> Data;
        internal string Json;
    }

    /// <summary>One read of the log: the kept events, where the next read starts, and how many the ring dropped unread.</summary>
    internal sealed class EventPage
    {
        internal List<GameEvent> Events;
        internal long Next;
        internal long Missed;
        internal bool More;
    }

    /// <summary>
    /// A bounded, thread-safe log of game events (hits, deaths, traces, reloads...) for /events and /scenario. Any thread
    /// may add; a waiting reader is woken by each new event.
    /// </summary>
    internal static class EventLog
    {
        private const int Capacity = 4000;

        private static readonly GameEvent[] Ring = new GameEvent[Capacity];
        private static readonly Dictionary<string, long> Counts = new Dictionary<string, long>();
        private static readonly object Gate = new object();
        private static long next = 1;
        private static int mainThread = -1;
        private static volatile bool recording;

        /// <summary>The number the next event will get; pass it back as since= to read only newer events.</summary>
        internal static long Next
        {
            get { lock (Gate) return next; }
        }

        /// <summary>The number of the oldest event the ring still holds (or of the next one when it is empty).</summary>
        internal static long Oldest
        {
            get { lock (Gate) return Math.Max(1, next - Capacity); }
        }

        internal static bool OnMainThread => Thread.CurrentThread.ManagedThreadId == mainThread;

        /// <summary>Whether the game's own events (hits, deaths, spawns, the player, bosses, log and console lines) are
        /// recorded: off until the first /events or /scenario call, so a game nobody tests pays one flag test per patch
        /// call. What the endpoints publish (trace, swap, reload...) is recorded either way.</summary>
        internal static bool Recording => recording;

        /// <summary>Starts recording the game's own events; called by /events and /scenario, it stays on for the session.</summary>
        internal static void StartRecording() => recording = true;

        /// <summary>Called once from the plugin's Awake, so events added on the main thread get the game time.</summary>
        internal static void SetMainThread() => mainThread = Thread.CurrentThread.ManagedThreadId;

        /// <summary>Records an event; kind is one lower-case word (hit, death, trace, reload...). The data is copied as plain
        /// data (Unity values become numbers or names), so the caller may keep changing its own dictionary.</summary>
        internal static void Add(string kind, Dictionary<string, object> data) => Keep(kind, EventData.Plain(data));

        /// <summary>Records an event whose data is plain already (strings, numbers, bools, number arrays and maps of them)
        /// and was made for this event alone: kept as it is, without the copy Add makes. The game patches' events use it.</summary>
        internal static void AddPlain(string kind, Dictionary<string, object> data) => Keep(kind, data ?? new Dictionary<string, object>());

        private static void Keep(string kind, Dictionary<string, object> data)
        {
            var added = new GameEvent { Kind = kind, Clock = DateTime.Now, Data = data };
            if (OnMainThread) added.GameTime = Fmt.R(Time.time);
            lock (Gate)
            {
                added.Seq = next;
                Ring[next % Capacity] = added;
                next++;
                Counts[kind] = Counts.TryGetValue(kind, out long count) ? count + 1 : 1;
                Monitor.PulseAll(Gate);
            }
        }

        /// <summary>Kept events numbered since or later, at most the newest limit of them.</summary>
        internal static List<GameEvent> Since(long since, int limit, Func<GameEvent, bool> keep)
        {
            List<GameEvent> result = Snapshot(since, out _);
            if (keep != null) result.RemoveAll(e => !keep(e));
            if (result.Count > limit) result.RemoveRange(0, result.Count - limit);
            return result;
        }

        /// <summary>Kept events numbered since or later, oldest first and at most max of them, with where to read next.</summary>
        internal static EventPage Read(long since, int max, Func<GameEvent, bool> keep)
        {
            List<GameEvent> all = Snapshot(since, out long end);
            long first = all.Count > 0 ? all[0].Seq : end;
            var page = new EventPage { Events = new List<GameEvent>(), Next = end, Missed = Math.Max(0, first - Math.Max(1, since)) };
            foreach (GameEvent e in all)
            {
                if (keep != null && !keep(e)) continue;
                page.Events.Add(e);
                if (page.Events.Count < max) continue;
                page.Next = e.Seq + 1;
                page.More = page.Next < end;
                break;
            }
            return page;
        }

        /// <summary>Blocks the calling thread (never the main thread) until an event numbered since or later exists.</summary>
        internal static bool WaitFor(long since, TimeSpan timeout)
        {
            DateTime end = DateTime.UtcNow + timeout;
            lock (Gate)
            {
                while (next <= since)
                {
                    TimeSpan left = end - DateTime.UtcNow;
                    if (left <= TimeSpan.Zero || !Monitor.Wait(Gate, left)) return next > since;
                }
                return true;
            }
        }

        /// <summary>How many events of each kind were added since the game started.</summary>
        internal static Dictionary<string, long> Tally()
        {
            lock (Gate) return new Dictionary<string, long>(Counts);
        }

        // The filter runs on the caller's copy, outside the lock, so a slow one never holds up the game adding events.
        private static List<GameEvent> Snapshot(long since, out long end)
        {
            lock (Gate)
            {
                end = next;
                long start = Math.Max(since, Math.Max(1, next - Capacity));
                var copy = new List<GameEvent>((int)Math.Max(0, end - start));
                for (long seq = start; seq < end; seq++) copy.Add(Ring[seq % Capacity]);
                return copy;
            }
        }
    }
}
