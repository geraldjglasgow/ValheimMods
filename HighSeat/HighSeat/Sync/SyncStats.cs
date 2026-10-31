using System.Diagnostics;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The send loop's numbers for load tests, published once a second, each for the second before (read them from
    /// DevBridge with /eval, for example HighSeat.SyncStats.MsPerSecond). Counting costs a few additions a send; the
    /// totals over the players are added up once a second.
    /// </summary>
    public static class SyncStats
    {
        /// <summary>True while HighSeat's send loop sends; false on a player's game and once it handed back to the game's.</summary>
        public static bool Active;

        /// <summary>Milliseconds spent in the send loop: round starts, hand-outs, full checks and packages.</summary>
        public static float MsPerSecond;

        /// <summary>The most milliseconds one frame spent in it.</summary>
        public static float MaxFrameMs;

        /// <summary>Rounds finished (every player served once).</summary>
        public static int RoundsPerSecond;

        /// <summary>Bytes of "ZDOData" packages sent, to all players together.</summary>
        public static int BytesPerSecond;

        /// <summary>The most bytes one player got.</summary>
        public static int MaxPeerBytesPerSecond;

        /// <summary>Objects sent, counted once per player they went to.</summary>
        public static int ObjectsPerSecond;

        /// <summary>Objects waiting for all players together (pending and set aside), at the moment of publishing.</summary>
        public static int PendingObjects;

        /// <summary>Players the loop serves.</summary>
        public static int Peers;

        /// <summary>Packages sent: a player served with something to send counts once a round.</summary>
        public static int PeersServed;

        /// <summary>Times a player with something to send was passed over because their socket's queue was full.</summary>
        public static int PeersSkipped;

        private static long _ticks;
        private static long _maxTicks;
        private static int _rounds;
        private static int _bytes;
        private static int _objects;
        private static int _served;
        private static int _skipped;
        private static float _next;

        internal static void Reset()
        {
            _ticks = 0;
            _maxTicks = 0;
            _rounds = 0;
            _bytes = 0;
            _objects = 0;
            _served = 0;
            _skipped = 0;
            _next = 0f;
        }

        /// <summary>One frame of the loop took this many Stopwatch ticks; publishes once a second.</summary>
        internal static void Frame(long ticks)
        {
            Active = true;
            _ticks += ticks;
            if (ticks > _maxTicks)
            {
                _maxTicks = ticks;
            }
            if (Time.unscaledTime >= _next)
            {
                Publish();
            }
        }

        internal static void Round()
        {
            _rounds++;
        }

        internal static void Sent(PeerSync ps, int bytes, int objects)
        {
            _served++;
            _bytes += bytes;
            _objects += objects;
            ps.BytesThisSecond += bytes;
        }

        internal static void Skipped()
        {
            _skipped++;
        }

        private static void Publish()
        {
            MsPerSecond = (float)(_ticks * 1000.0 / Stopwatch.Frequency);
            MaxFrameMs = (float)(_maxTicks * 1000.0 / Stopwatch.Frequency);
            RoundsPerSecond = _rounds;
            BytesPerSecond = _bytes;
            ObjectsPerSecond = _objects;
            PeersServed = _served;
            PeersSkipped = _skipped;
            PeerTotals();
            Reset();
            _next = Time.unscaledTime + 1f;
        }

        private static void PeerTotals()
        {
            int pending = 0;
            int most = 0;
            foreach (PeerSync ps in SyncPeers.All)
            {
                pending += ps.Pending.Count + Backlog.Left(ps);
                most = Mathf.Max(most, ps.BytesThisSecond);
                ps.BytesThisSecond = 0;
            }
            PendingObjects = pending;
            MaxPeerBytesPerSecond = most;
            Peers = SyncPeers.All.Count;
        }
    }
}
