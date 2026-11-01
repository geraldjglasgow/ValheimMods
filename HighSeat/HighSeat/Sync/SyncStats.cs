using System.Diagnostics;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The send loop's numbers for load tests, published once a second, each for the second before (read them from
    /// DevBridge with /eval, for example HighSeat.SyncStats.MsPerSecond). Counting costs a few additions a send; the
    /// totals over the players are added up once a second. Main thread only.
    /// </summary>
    public static class SyncStats
    {
        /// <summary>True while HighSeat's send loop sends; false on a player's game and once it handed back to the game's.</summary>
        public static bool Active;

        /// <summary>Milliseconds spent in the send loop: round starts, hand-outs, full checks and packages.</summary>
        public static float MsPerSecond;

        /// <summary>The most milliseconds one frame spent in it.</summary>
        public static float MaxFrameMs;

        /// <summary>Of <see cref="MsPerSecond"/>: handing changed objects out to the players near them.</summary>
        public static float HandOutMsPerSecond;

        /// <summary>Of <see cref="MsPerSecond"/>: the main thread waiting for (and helping with) the batches' preparation.</summary>
        public static float PrepMsPerSecond;

        /// <summary>Of <see cref="MsPerSecond"/>: writing and sending the packages, on the main thread.</summary>
        public static float SendMsPerSecond;

        /// <summary>The most threads one batch was prepared on, the main thread included (1: the main thread alone).</summary>
        public static int Lanes;

        /// <summary>Pending objects passed over with one lookup while the update rate held them, counted per player.</summary>
        public static int HeldPerSecond;

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

        /// <summary>Records made for sending (each made once, then copied to every player it serves).</summary>
        public static int RecordsPerSecond;

        /// <summary>Of <see cref="RecordsPerSecond"/>: made ahead on the send threads, the main thread only copied them.</summary>
        public static int RecordsAheadPerSecond;

        /// <summary>Of <see cref="RecordsPerSecond"/>: made with the game's own ZDO.Serialize (the record writer off or unable).</summary>
        public static int RecordsByGamePerSecond;

        /// <summary>Players the loop serves.</summary>
        public static int Peers;

        /// <summary>Packages sent: a player served with something to send counts once a round.</summary>
        public static int PeersServed;

        /// <summary>Times a player with something to send was passed over because their socket's queue was full.</summary>
        public static int PeersSkipped;

        private static long _ticks;
        private static long _maxTicks;
        private static long _handOutTicks;
        private static long _prepTicks;
        private static long _sendTicks;
        private static int _lanes;
        private static int _held;
        private static int _rounds;
        private static int _bytes;
        private static int _objects;
        private static int _served;
        private static int _skipped;
        private static int _records;
        private static int _recordsAhead;
        private static int _recordsByGame;
        private static float _next;

        internal static void Reset()
        {
            _ticks = 0;
            _maxTicks = 0;
            _handOutTicks = 0;
            _prepTicks = 0;
            _sendTicks = 0;
            _lanes = 0;
            _held = 0;
            _rounds = 0;
            _bytes = 0;
            _objects = 0;
            _served = 0;
            _skipped = 0;
            _records = 0;
            _recordsAhead = 0;
            _recordsByGame = 0;
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

        internal static void HandedOut(long ticks)
        {
            _handOutTicks += ticks;
        }

        /// <summary>A batch of sends: the threads it was prepared on, its ticks to prepare and to send, the holds met.</summary>
        internal static void Batch(int lanes, long prepTicks, long sendTicks, int held)
        {
            _prepTicks += prepTicks;
            _sendTicks += sendTicks;
            _held += held;
            if (lanes > _lanes)
            {
                _lanes = lanes;
            }
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

        /// <summary>A record made: ahead on a send thread, or by the game's own code, or (neither) by the writer here.</summary>
        internal static void Record(bool ahead, bool byGame)
        {
            _records++;
            _recordsAhead += ahead ? 1 : 0;
            _recordsByGame += byGame ? 1 : 0;
        }

        private static void Publish()
        {
            MsPerSecond = Ms(_ticks);
            MaxFrameMs = Ms(_maxTicks);
            HandOutMsPerSecond = Ms(_handOutTicks);
            PrepMsPerSecond = Ms(_prepTicks);
            SendMsPerSecond = Ms(_sendTicks);
            Lanes = _lanes;
            HeldPerSecond = _held;
            RoundsPerSecond = _rounds;
            BytesPerSecond = _bytes;
            ObjectsPerSecond = _objects;
            PeersServed = _served;
            PeersSkipped = _skipped;
            RecordsPerSecond = _records;
            RecordsAheadPerSecond = _recordsAhead;
            RecordsByGamePerSecond = _recordsByGame;
            PeerTotals();
            Reset();
            _next = Time.unscaledTime + 1f;
        }

        private static float Ms(long ticks) => (float)(ticks * 1000.0 / Stopwatch.Frequency);

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
