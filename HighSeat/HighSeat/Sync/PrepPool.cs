using System;
using System.Diagnostics;
using System.Threading;

namespace HighSeat
{
    /// <summary>
    /// Worker threads for the per-player part of a send (<see cref="SendPrep"/>), so a crowded round prepares several
    /// players at once: about half of a player's send with 100 players. Fork and join inside the send loop's frame time:
    /// the main thread hands a batch of players (<see cref="SendSlot"/>) to min(<see cref="MaxWorkers"/>, cores - 1)
    /// persistent threads (none with fewer than <see cref="MinCores"/> logical cores, nor for a batch of fewer than
    /// <see cref="MinBatch"/>), prepares players itself as well, waits until every thread that joined in is done, and only
    /// then writes and sends the packages (<see cref="PeerSend"/>, on the main thread as before). Players are taken one at
    /// a time from a shared counter, so a slow one (a backlog to sort) does not hold up the rest, and a thread that wakes
    /// only after the main thread has finished the batch stays out of it, so a busy machine (a host's game drawing) never
    /// makes the main thread wait for a thread that has not started. Between batches each thread waits on its own event;
    /// nothing is allocated per batch and no thread pool work item is made.
    ///
    /// While the threads run the main thread does nothing but prepare players too: the game runs no code, so nothing in it
    /// changes and the threads only read it. What a thread reads, all written only on the main thread: ZDOMan.m_objectsByID
    /// (read directly rather than through ZDOMan.GetZDO, so no other mod's patch on that runs off the main thread); each
    /// object's position, data and owner revisions, type and Distant flag, and its owner (ZDOExtraData's owner table and
    /// ZDOID's list of users); the player's ZDOPeer (m_zdos, m_forceSend, and of m_peer the reference position and uid);
    /// the player's area (<see cref="PeerSync.Zone"/> and its <see cref="AreaShape"/> table, both made on the main
    /// thread); the round's number and start times (<see cref="Throttle"/>, set at the round's start); and the Time.time
    /// read before the fork (no Unity API is called off the main thread). What a thread writes is the send loop's own: the
    /// player's pending set, backlog and holds (<see cref="Holds"/>; a player is in one slot of a batch, so in one thread),
    /// the slot's list and its own buffers (crowd ranks, send orders, candidates, drops, backlog sort). The hand-out of
    /// changes, the full checks, the force-send list and every package run on the main thread before or after a batch,
    /// never during one. The game's own other threads (the world save, terrain and liquid builders, compression for
    /// crossplay) touch none of it. Right after preparing a player a thread makes ahead the records and deltas their
    /// package will need (<see cref="Ahead"/>), which reads besides: the kept records and the deltas worked out from them
    /// (<see cref="Records"/>' table and buffer, <see cref="DeltaCache"/>), and the records sent before
    /// (<see cref="History"/>), all written only by the main thread's packages, after the join; what a record is made of
    /// (ZDOExtraData's value, connection and owner tables, ZDOID's list of users, the object's rotation, prefab and flags,
    /// all written only by game code on the main thread); and the player's drop and force-send sets (m_invalidSector,
    /// m_forceSend). It writes only its own buffers and the entries it claims in <see cref="Premade"/>'s table, which the
    /// main thread reads after the join, and the history's age limit uses the time read on the main thread before the
    /// fork (<see cref="Premade.Now"/>).
    ///
    /// A thread that throws gives back its player: the main thread then prepares whatever was not finished, and for the
    /// rest of the session prepares alone, logged once. A batch still unfinished after <see cref="HangSeconds"/> s means a
    /// thread is stuck: the game's own send loop takes over for as long as the game runs.
    /// </summary>
    internal static class PrepPool
    {
        private const int MaxWorkers = 4;
        private const int MinCores = 3;
        private const int MinBatch = 2;
        private const int HangSeconds = 5;
        private const double SpinMicroseconds = 100.0;

        // _state: how many threads are in the open batch, plus Closed once the main thread has finished its share.
        private const int Closed = 1 << 30;

        private static readonly long SpinTicks = (long)(Stopwatch.Frequency * SpinMicroseconds / 1e6);
        private static SendPrep[] _lanes = new SendPrep[0];
        private static Ahead[] _ahead = new Ahead[0];
        private static ManualResetEventSlim[] _go = new ManualResetEventSlim[0];
        private static int _workers = -1;
        private static bool _alone;
        private static volatile bool _lost;
        private static int _state = Closed;
        private static ZDOMan _man;
        private static SendSlot[] _slots = new SendSlot[0];
        private static int _count;
        private static float _time;
        private static int _taken;
        private static Exception _error;

        /// <summary>True once a batch never finished: the game's own loop sends for as long as the game runs.</summary>
        public static bool Hung { get; private set; }

        /// <summary>Threads a full batch is prepared on, the main thread included: 1 when it prepares alone.</summary>
        public static int Lanes => _workers > 0 && !_alone && !_lost ? _workers + 1 : 1;

        /// <summary>Threads a batch of this many players is prepared on, the main thread included.</summary>
        public static int LanesFor(int players) => players >= MinBatch ? Math.Min(players, Lanes) : 1;

        /// <summary>A new session: the threads are started the first time, and are used again after a failure.</summary>
        public static void Begin()
        {
            Start();
            _alone = false;
        }

        /// <summary>Prepares every slot's list; on return all are prepared, or this threw and the loop hands back.</summary>
        public static void Prepare(ZDOMan man, SendSlot[] slots, int count, float time)
        {
            Start();
            Premade.Batch(LanesFor(count));
            if (LanesFor(count) == 1)
            {
                Alone(man, slots, count, time);
                return;
            }
            Fork(man, slots, count, time);
            try
            {
                RunLane(0);
            }
            finally
            {
                Join();
            }
            Premade.Joined();
            Recover(man, slots, count, time);
        }

        /// <summary>The pending hold counts of every thread, added up and zeroed (main thread, between batches).</summary>
        public static int TakeHoldHits()
        {
            int hits = 0;
            for (int i = 0; i <= _workers; i++)
            {
                hits += _lanes[i].HoldHits;
                _lanes[i].HoldHits = 0;
            }
            return hits;
        }

        /// <summary>
        /// Lets go of every ZDO reference the threads' buffers hold (main thread, between batches); after a hang they are
        /// left to the stuck thread.
        /// </summary>
        public static void Clear()
        {
            for (int i = 0; i <= _workers && !Hung; i++)
            {
                _lanes[i].Clear();
            }
        }

        // The first time: the main thread's buffers and the threads, as many as the machine has cores to spare.
        private static void Start()
        {
            if (_workers >= 0)
            {
                return;
            }
            _workers = 0;
            int cores = Environment.ProcessorCount;
            int wanted = cores < MinCores ? 0 : Math.Min(MaxWorkers, cores - 1);
            _lanes = new SendPrep[wanted + 1];
            _lanes[0] = new SendPrep();
            _ahead = new Ahead[wanted + 1];
            _ahead[0] = new Ahead(0);
            _go = new ManualResetEventSlim[wanted];
            Launch(wanted);
            Premade.Lanes(Array.ConvertAll(_ahead, ahead => ahead?.Buffer));
            HighSeat.Log.LogInfo($"sends are prepared on {_workers} threads besides the main thread ({cores} cores)");
        }

        // Starts the threads one by one; one that cannot be started leaves the ones before it (or none) in use.
        private static void Launch(int wanted)
        {
            try
            {
                for (int lane = 1; lane <= wanted; lane++)
                {
                    _lanes[lane] = new SendPrep();
                    _ahead[lane] = new Ahead(lane);
                    _go[lane - 1] = new ManualResetEventSlim(false);
                    Thread thread = new Thread(Work) { IsBackground = true, Name = "HighSeat send " + lane };
                    thread.Start(lane);
                    _workers = lane;
                }
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"only {_workers} send threads could be started: {e.Message}");
            }
        }

        private static void Alone(ZDOMan man, SendSlot[] slots, int count, float time)
        {
            for (int i = 0; i < count; i++)
            {
                _lanes[0].Run(man, slots[i], time);
                slots[i].Prepared = true;
            }
        }

        // What the threads read is written before the batch opens (a volatile write; they join it with an interlocked
        // compare, so they see it), then as many threads are woken as there are players besides the main thread's first.
        private static void Fork(ZDOMan man, SendSlot[] slots, int count, float time)
        {
            _man = man;
            _slots = slots;
            _count = count;
            _time = time;
            _taken = 0;
            _error = null;
            Volatile.Write(ref _state, 0);
            int use = LanesFor(count) - 1;
            for (int i = 0; i < use; i++)
            {
                _go[i].Set();
            }
        }

        // Closes the batch to threads not yet in it, then waits for those in it.
        private static void Join()
        {
            if (Interlocked.Add(ref _state, Closed) == Closed)
            {
                return;
            }
            long start = Stopwatch.GetTimestamp();
            while (Volatile.Read(ref _state) != Closed)
            {
                Pause(Stopwatch.GetTimestamp() - start);
            }
        }

        // A short spin first, then giving up the core (a thread waiting for one gets it); a batch this late is stuck.
        private static void Pause(long waited)
        {
            if (waited > Stopwatch.Frequency * HangSeconds)
            {
                Hung = true;
                throw new TimeoutException($"the send threads did not finish a batch within {HangSeconds} s");
            }
            if (waited < SpinTicks)
            {
                Thread.SpinWait(20);
            }
            else
            {
                Thread.Sleep(0);
            }
        }

        // After a failure, the main thread prepares what was left over and goes on alone for the session.
        private static void Recover(ZDOMan man, SendSlot[] slots, int count, float time)
        {
            if (_error == null)
            {
                return;
            }
            HighSeat.Log.LogWarning($"preparing a send on a thread failed, so the main thread prepares alone for the rest of this session: {_error}");
            _alone = true;
            _error = null;
            Clear();
            for (int i = 0; i < count; i++)
            {
                if (!slots[i].Prepared)
                {
                    slots[i].ToSend.Clear();
                    _lanes[0].Run(man, slots[i], time);
                    slots[i].Prepared = true;
                }
            }
        }

        // Takes players one at a time until none are left; a failure leaves its player unprepared and stops this lane.
        private static void RunLane(int lane)
        {
            SendPrep prep = _lanes[lane];
            int k;
            while ((k = Interlocked.Increment(ref _taken) - 1) < _count)
            {
                try
                {
                    prep.Run(_man, _slots[k], _time);
                    _slots[k].Prepared = true;
                    if (Premade.Open)
                    {
                        _ahead[lane].Walk(_man, _slots[k]);
                    }
                }
                catch (Exception e)
                {
                    Interlocked.CompareExchange(ref _error, e, null);
                    return;
                }
            }
        }

        // A thread's life: wait to be woken, take a share if the batch is still open. If it ever ends (the game closing),
        // every batch from then on is prepared on the main thread.
        private static void Work(object state)
        {
            int lane = (int)state;
            ManualResetEventSlim go = _go[lane - 1];
            try
            {
                while (true)
                {
                    go.Wait();
                    go.Reset();
                    Shift(lane);
                }
            }
            catch (Exception)
            {
                _lost = true;
            }
        }

        private static void Shift(int lane)
        {
            if (!JoinIn())
            {
                return;
            }
            try
            {
                RunLane(lane);
            }
            catch (Exception e)
            {
                Interlocked.CompareExchange(ref _error, e, null);
            }
            finally
            {
                Interlocked.Decrement(ref _state);
            }
        }

        // Counts this thread into the open batch; false once the main thread has closed it (it then stays out).
        private static bool JoinIn()
        {
            int seen = Volatile.Read(ref _state);
            while ((seen & Closed) == 0)
            {
                int was = Interlocked.CompareExchange(ref _state, seen + 1, seen);
                if (was == seen)
                {
                    return true;
                }
                seen = was;
            }
            return false;
        }
    }
}
