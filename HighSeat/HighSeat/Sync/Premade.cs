using System;
using System.Threading;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// What a batch of sends will need that the send threads made ahead while the batch was prepared (<see cref="Ahead"/>,
    /// <see cref="PrepPool"/>): each object's record and the deltas from the revisions its players hold, so the main
    /// thread, writing the packages after, only copies them (<see cref="Records"/>, <see cref="DeltaWriter"/>). A table
    /// keyed by the object (the ZDO itself, one per id while the batch runs), a tag (0 for the record, the revisions a
    /// player holds for a delta) and, for a delta, which record it leads to (a kept one's place, or the batch's own). The
    /// threads share it without locks: an entry is claimed with one interlocked compare-exchange, keyed by the thread that
    /// claimed it and published with a volatile write, then filled and published again once made; a thread that meets an
    /// entry still being keyed takes it for another key (at worst the same thing is made twice), and one that meets an
    /// entry not yet made does not wait for it. What is made lies in the claiming thread's own buffer. Open addressing: a
    /// key is looked for at most <see cref="MaxProbes"/> places on from its own, and at most <see cref="MostClaims"/>
    /// entries are claimed a batch (half the table); past either nothing more is made ahead and the main thread makes the
    /// rest, as before. The main thread reads the table only after the batch's join (every thread's writes are
    /// visible then) and empties it after the batch's sends, the claimed entries only, so no ZDO reference outlives the
    /// frame. Made ahead only while the record writer is on (<see cref="RecordChecks"/>) and only for a batch prepared on
    /// more than one thread; a thread that throws here turns it off for the rest of the session, logged once.
    /// </summary>
    internal static class Premade
    {
        private const int Capacity = 1 << 15;
        private const int Mask = Capacity - 1;
        private const int MostClaims = Capacity / 2;
        private const int MaxProbes = 64;

        private const int Empty = 0;
        private const int Keying = 1;
        private const int Keyed = 2;

        private const int Waiting = 0;
        private const int Made = 1;
        private const int Failed = -1;

        /// <summary>The <c>of</c> of a delta that leads to the batch's own record of the object.</summary>
        public const int BatchRecord = -1;

        // The key (Key, Tag, Of) is set while State is Keying; what was made (Lane's buffer from Start, Length bytes: a
        // delta of 0 means the whole record goes) once Done is Made. Extra: for a record, where the main thread put it in
        // the kept records (-1 until then); for a delta, the history place of the held record it starts from.
        private struct Entry
        {
            public int State;
            public int Done;
            public ZDO Key;
            public long Tag;
            public int Of;
            public int Lane;
            public int Start;
            public int Length;
            public int Extra;
        }

        private static readonly Entry[] Entries = new Entry[Capacity];
        private static readonly int[] Used = new int[Capacity];
        private static RecordBuffer[] _buffers = new RecordBuffer[0];
        private static int _used;
        private static volatile bool _open;
        private static bool _broken;
        private static Exception _error;

        /// <summary>True while the threads preparing this batch make ahead (set by the main thread before the fork).</summary>
        public static bool Open => _open;

        /// <summary>The batch's Time.unscaledTime, read on the main thread before the fork, for the history's age limit.</summary>
        public static float Now { get; private set; }

        /// <summary>The threads' buffers, by lane (the main thread's first), once when the threads are started.</summary>
        public static void Lanes(RecordBuffer[] buffers) => _buffers = buffers;

        /// <summary>A new session: nothing kept from the last, and a failure in it forgotten.</summary>
        public static void Begin()
        {
            Array.Clear(Entries, 0, Entries.Length);
            _used = 0;
            _broken = false;
            _error = null;
        }

        /// <summary>Before a batch's fork (main thread): whether its threads make ahead.</summary>
        public static void Batch(int lanes)
        {
            Now = Time.unscaledTime;
            _open = lanes > 1 && !_broken && RecordChecks.On;
        }

        /// <summary>The tag of a delta from these held revisions (never 0, which is the record's).</summary>
        public static long DeltaTag(uint data, ushort owner) => ((long)data << 16 | owner) + 1;

        /// <summary>
        /// How a delta made ahead names the record it leads to (main thread): <see cref="BatchRecord"/> when the record at
        /// <paramref name="start"/> is where the main thread put the batch's record of the object, else that place.
        /// </summary>
        public static int RecordOf(ZDO zdo, int start)
        {
            int at = Find(zdo, 0L, 0);
            return at >= 0 && Entries[at].Extra == start ? BatchRecord : start;
        }

        /// <summary>
        /// Finds the entry of this key, or claims a new one for it (<paramref name="mine"/>: this thread makes it); -1
        /// when the table is too full here. Any thread.
        /// </summary>
        public static int Claim(ZDO zdo, long tag, int of, out bool mine)
        {
            mine = false;
            int at = Slot(zdo, tag, of);
            for (int probe = 0; probe < MaxProbes; probe++, at = (at + 1) & Mask)
            {
                if (Volatile.Read(ref Entries[at].State) == Empty && TryKey(at, zdo, tag, of))
                {
                    mine = true;
                    return at;
                }
                int state = Volatile.Read(ref Entries[at].State);
                if (state == Empty)
                {
                    return -1;
                }
                if (state == Keyed && Matches(at, zdo, tag, of))
                {
                    return at;
                }
            }
            return -1;
        }

        /// <summary>The claiming thread: what it made, in its own buffer, published to the others.</summary>
        public static void Publish(int at, int lane, int start, int length, int extra)
        {
            Entries[at].Lane = lane;
            Entries[at].Start = start;
            Entries[at].Length = length;
            Entries[at].Extra = extra;
            Volatile.Write(ref Entries[at].Done, Made);
        }

        /// <summary>The claiming thread could not make it: the main thread makes it, as before.</summary>
        public static void Fail(int at) => Volatile.Write(ref Entries[at].Done, Failed);

        /// <summary>The length of what an entry holds once made, else -1 (not made yet, or failed). Any thread.</summary>
        public static int LengthIfMade(int at) => Volatile.Read(ref Entries[at].Done) == Made ? Entries[at].Length : -1;

        /// <summary>A thread failed while making ahead: the main thread turns it off after the join.</summary>
        public static void Broke(Exception e) => Interlocked.CompareExchange(ref _error, e, null);

        /// <summary>After the join (main thread): a failure on a thread turns making ahead off for the session, logged once.</summary>
        public static void Joined()
        {
            Exception error = Interlocked.Exchange(ref _error, null);
            if (error == null || _broken)
            {
                return;
            }
            _broken = true;
            HighSeat.Log.LogWarning($"making records ahead on a send thread failed, so the main thread makes them for the rest of this session: {error}");
        }

        /// <summary>The made entry of this key, or -1 (main thread, after the join).</summary>
        public static int Find(ZDO zdo, long tag, int of)
        {
            if (_used == 0)
            {
                return -1;
            }
            int at = Slot(zdo, tag, of);
            for (int probe = 0; probe < MaxProbes && Entries[at].State != Empty; probe++, at = (at + 1) & Mask)
            {
                if (Entries[at].State == Keyed && Entries[at].Done == Made && Matches(at, zdo, tag, of))
                {
                    return at;
                }
            }
            return -1;
        }

        /// <summary>The buffer an entry's bytes lie in, at <see cref="StartOf"/>.</summary>
        public static byte[] BytesOf(int at) => _buffers[Entries[at].Lane].Bytes;

        public static int StartOf(int at) => Entries[at].Start;

        public static int LengthOf(int at) => Entries[at].Length;

        public static int ExtraOf(int at) => Entries[at].Extra;

        /// <summary>The main thread notes something on an entry (where it put a record it took).</summary>
        public static void Note(int at, int extra) => Entries[at].Extra = extra;

        /// <summary>After a batch's sends (main thread): the claimed entries and the threads' buffers emptied.</summary>
        public static void Clear()
        {
            int used = Math.Min(_used, Capacity);
            for (int i = 0; i < used; i++)
            {
                Entries[Used[i]] = default;
            }
            _used = 0;
            _open = false;
            foreach (RecordBuffer buffer in _buffers)
            {
                buffer?.Clear();
            }
        }

        // Claims an empty entry and keys it; false when another thread took it first, or the batch has claimed its most.
        private static bool TryKey(int at, ZDO zdo, long tag, int of)
        {
            if (Volatile.Read(ref _used) >= MostClaims || Interlocked.CompareExchange(ref Entries[at].State, Keying, Empty) != Empty)
            {
                return false;
            }
            Entries[at].Key = zdo;
            Entries[at].Tag = tag;
            Entries[at].Of = of;
            Entries[at].Done = Waiting;
            Used[Interlocked.Increment(ref _used) - 1] = at;
            Volatile.Write(ref Entries[at].State, Keyed);
            return true;
        }

        private static bool Matches(int at, ZDO zdo, long tag, int of)
        {
            return ReferenceEquals(Entries[at].Key, zdo) && Entries[at].Tag == tag && Entries[at].Of == of;
        }

        private static int Slot(ZDO zdo, long tag, int of)
        {
            uint hash = zdo.m_uid.ID * 2654435761u ^ (uint)tag * 2246822519u ^ (uint)(tag >> 32) * 3266489917u ^ (uint)of * 668265263u;
            return (int)(hash ^ hash >> 15) & Mask;
        }
    }
}
