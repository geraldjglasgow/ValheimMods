#if DEBUG
using System.Diagnostics;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The delta sends' numbers for load tests, published once a second, each for the second before (DevBridge's /load
    /// measure reads every public static field here, as it reads <see cref="SyncStats"/>). Only the players who read
    /// deltas (HighSeatPlus) count; everyone else gets the game's format and counts only in SyncStats. Counting costs a
    /// few additions per object sent.
    /// </summary>
    public static class DeltaStats
    {
        /// <summary>Players who said hello in the delta format and are still connected (or joining).</summary>
        public static int Players;

        /// <summary>Objects sent as deltas, counted once per player they went to.</summary>
        public static int DeltasPerSecond;

        /// <summary>Objects sent whole to those players: new to them, no earlier record kept, or the delta no smaller.</summary>
        public static int WholePerSecond;

        /// <summary>Bytes of deltas, and of whole records, sent to those players (kind bytes included).</summary>
        public static int DeltaBytesPerSecond;

        public static int WholeBytesPerSecond;

        /// <summary>Bytes the deltas saved against sending those objects whole.</summary>
        public static int SavedBytesPerSecond;

        /// <summary>Deltas worked out on the main thread (a diff run); the rest came from the cache or a send thread.</summary>
        public static int DiffsPerSecond;

        /// <summary>Milliseconds spent working deltas out on the main thread.</summary>
        public static float DiffMsPerSecond;

        /// <summary>Deltas taken from a send thread that worked them out ahead (no diff run on the main thread).</summary>
        public static int DiffsAheadPerSecond;

        /// <summary>Whole records sent because the record the player held was no longer kept.</summary>
        public static int NoBasePerSecond;

        /// <summary>Objects players asked to have sent whole again (their copy was not the one the delta was for).</summary>
        public static int AskedPerSecond;

        /// <summary>Objects with a record kept, and the kilobytes kept, at the moment of publishing.</summary>
        public static int HistoryObjects;

        public static int HistoryKilobytes;

        private static int _deltas;
        private static int _whole;
        private static int _deltaBytes;
        private static int _wholeBytes;
        private static int _saved;
        private static int _diffs;
        private static int _diffsAhead;
        private static long _diffTicks;
        private static int _noBase;
        private static int _asked;
        private static float _next;

        internal static void Delta(int bytes, int wholeBytes)
        {
            _deltas++;
            _deltaBytes += bytes;
            _saved += wholeBytes - bytes;
        }

        internal static void Whole(int bytes)
        {
            _whole++;
            _wholeBytes += bytes;
        }

        internal static void Diffed(long ticks)
        {
            _diffs++;
            _diffTicks += ticks;
        }

        internal static void NoBase() => _noBase++;

        internal static void Ahead() => _diffsAhead++;

        internal static void Asked(int count) => _asked += count;

        /// <summary>Each frame the send loop sends in; publishes once a second.</summary>
        internal static void Frame()
        {
            if (Time.unscaledTime < _next)
            {
                return;
            }
            _next = Time.unscaledTime + 1f;
            DeltasPerSecond = _deltas;
            WholePerSecond = _whole;
            DeltaBytesPerSecond = _deltaBytes;
            WholeBytesPerSecond = _wholeBytes;
            SavedBytesPerSecond = _saved;
            DiffsPerSecond = _diffs;
            DiffMsPerSecond = (float)(_diffTicks * 1000.0 / Stopwatch.Frequency);
            DiffsAheadPerSecond = _diffsAhead;
            NoBasePerSecond = _noBase;
            AskedPerSecond = _asked;
            Totals();
            Reset();
        }

        private static void Totals()
        {
            Players = DeltaPeers.Count;
            HistoryObjects = History.Objects;
            HistoryKilobytes = History.Kilobytes;
        }

        private static void Reset()
        {
            _deltas = 0;
            _whole = 0;
            _deltaBytes = 0;
            _wholeBytes = 0;
            _saved = 0;
            _diffs = 0;
            _diffsAhead = 0;
            _diffTicks = 0;
            _noBase = 0;
            _asked = 0;
        }
    }
}
#endif
