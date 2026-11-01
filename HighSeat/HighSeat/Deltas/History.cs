using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The records the server actually sent, kept a while so a later update can go as only what changed since the one a
    /// player holds (<see cref="DeltaWriter"/>). Every record <see cref="Records"/> makes is kept here, header and data
    /// byte for byte, while any player says it reads deltas (<see cref="DeltaPeers"/>), and found again by the object
    /// and the data and owner revisions the player is booked at (ZDOPeer.m_zdos). Two rings, allocated once on first
    /// use and let go when the world closes: the bytes (<see cref="ByteCapacity"/>) and the entries
    /// (<see cref="EntryCapacity"/>), each entry linked to the one before it of the same object; a newer entry
    /// overwrites the oldest, which leaves the table of each object's latest entry if it was that. A record past the
    /// end of the byte ring starts at its beginning (the tail is skipped), so a record is always in one piece, and is
    /// readable while no later record has come round onto it. A search walks at most <see cref="MaxWalk"/> records of the
    /// object back and none older than <see cref="MaxAge"/> s; a record of the same revisions and bytes as the object's
    /// latest is not kept twice (the record cache is emptied every 0.25 s and makes the same record again), one of the
    /// same revisions but other bytes makes the latest unsure (never a base), and none over <see cref="MaxRecord"/>
    /// bytes is kept. With 100 players in a crowd the server makes about 2,000 records a second of about 600 bytes: the
    /// ring holds about 7 s of them; nothing is allocated per record. Written only on the main thread (by the records
    /// made in its packages, and when the world closes), so the send threads may find records in it while the main
    /// thread waits for a batch (<see cref="Ahead"/>), with the time handed to them.
    /// </summary>
    internal static class History
    {
        private const int ByteCapacity = 8 * 1024 * 1024;
        private const int EntryCapacity = 65536;
        private const int MaxWalk = 32;
        private const float MaxAge = 10f;
        private const int MaxRecord = 64 * 1024;

        private struct Entry
        {
            public ZDOID Id;
            public uint Data;
            public ushort Owner;
            public bool Unsure;
            public int Length;
            public float Time;
            public long At;
            public long Previous;
        }

        private static readonly Dictionary<ZDOID, long> Latest = new Dictionary<ZDOID, long>();
        private static byte[] _bytes = null!;
        private static Entry[] _entries = null!;
        private static long _head;
        private static long _serial;

        /// <summary>The byte ring; a found record is at the offset <see cref="Find"/> gave.</summary>
        public static byte[] Bytes => _bytes;

        /// <summary>Objects with at least one record kept.</summary>
        public static int Objects => Latest.Count;

        /// <summary>Kilobytes of records the byte ring holds now.</summary>
        public static int Kilobytes => (int)(Math.Min(_head, ByteCapacity) / 1024);

        /// <summary>Keeps a copy of a record just made (header and data), with the revisions it carries.</summary>
        public static void Keep(ZDOID id, byte[] source, int at, int length, uint data, ushort owner)
        {
            if (length > MaxRecord)
            {
                return;
            }
            if (_bytes == null)
            {
                _bytes = new byte[ByteCapacity];
                _entries = new Entry[EntryCapacity];
            }
            if (AtLatestRevisions(id, source, at, length, data, owner))
            {
                return;
            }
            long where = Place(length);
            Buffer.BlockCopy(source, at, _bytes, (int)(where % ByteCapacity), length);
            Add(id, data, owner, where, length);
        }

        /// <summary>Where the record of this object at these revisions lies in <see cref="Bytes"/>, if it is still kept.</summary>
        public static bool Find(ZDOID id, uint data, ushort owner, out int at, out int length)
        {
            return Find(id, data, owner, Time.unscaledTime, out at, out length);
        }

        /// <summary>
        /// <see cref="Find"/> at a time read on the main thread (Time.unscaledTime): for the send threads while the main
        /// thread waits for a batch, when nothing writes the history.
        /// </summary>
        public static bool Find(ZDOID id, uint data, ushort owner, float now, out int at, out int length)
        {
            at = 0;
            length = 0;
            long serial = Walk(id, data, owner, now);
            if (serial < 0)
            {
                return false;
            }
            Entry entry = _entries[serial % EntryCapacity];
            at = (int)(entry.At % ByteCapacity);
            length = entry.Length;
            return true;
        }

        // The serial of the object's newest record at these revisions within the walk and age limits, else -1.
        private static long Walk(ZDOID id, uint data, ushort owner, float now)
        {
            if (_bytes == null || !Latest.TryGetValue(id, out long serial))
            {
                return -1;
            }
            float oldest = now - MaxAge;
            for (int step = 0; step < MaxWalk && Readable(serial); step++)
            {
                Entry entry = _entries[serial % EntryCapacity];
                if (entry.Time < oldest)
                {
                    return -1;
                }
                if (entry.Data == data && entry.Owner == owner)
                {
                    return entry.Unsure ? -1 : serial;
                }
                serial = entry.Previous;
            }
            return -1;
        }

        /// <summary>Lets go of everything (the world closed): the rings are freed and made again on the next record.</summary>
        public static void Clear()
        {
            Latest.Clear();
            _bytes = null!;
            _entries = null!;
            _head = 0;
            _serial = 0;
        }

        // Not yet overwritten: the entry by a newer entry in its slot, its bytes by a newer record come round onto them.
        private static bool Readable(long serial)
        {
            if (serial < 0 || serial < _serial - EntryCapacity)
            {
                return false;
            }
            return _head - _entries[serial % EntryCapacity].At <= ByteCapacity;
        }

        // True when the object's latest kept record has these revisions: the same bytes need no second copy, and other
        // bytes at the same revisions (a value removed without a revision, ZDO.RemoveFloat and the like) leave no record
        // a player at those revisions surely holds, so that record is marked unsure (never a base: those players get the
        // object whole) until the object's revisions move on.
        private static bool AtLatestRevisions(ZDOID id, byte[] source, int at, int length, uint data, ushort owner)
        {
            if (!Latest.TryGetValue(id, out long serial) || !Readable(serial))
            {
                return false;
            }
            Entry entry = _entries[serial % EntryCapacity];
            if (entry.Data != data || entry.Owner != owner)
            {
                return false;
            }
            if (entry.Length != length || !RecordBytes.Same(_bytes, (int)(entry.At % ByteCapacity), source, at, length))
            {
                _entries[serial % EntryCapacity].Unsure = true;
            }
            return true;
        }

        // The ring position for a record of this length: the next free one, or the ring's start when it would run past the end.
        private static long Place(int length)
        {
            long offset = _head % ByteCapacity;
            if (offset + length > ByteCapacity)
            {
                _head += ByteCapacity - offset;
            }
            long where = _head;
            _head += length;
            return where;
        }

        private static void Add(ZDOID id, uint data, ushort owner, long where, int length)
        {
            long serial = _serial++;
            int slot = (int)(serial % EntryCapacity);
            if (serial >= EntryCapacity)
            {
                Evict(_entries[slot], serial - EntryCapacity);
            }
            long previous = Latest.TryGetValue(id, out long last) ? last : -1;
            _entries[slot] = new Entry
            {
                Id = id, Data = data, Owner = owner, Length = length, Time = Time.unscaledTime, At = where, Previous = previous,
            };
            Latest[id] = serial;
        }

        // The entry leaving its slot leaves the table of latest entries if it was its object's latest.
        private static void Evict(Entry old, long serial)
        {
            if (Latest.TryGetValue(old.Id, out long latest) && latest == serial)
            {
                Latest.Remove(old.Id);
            }
        }
    }
}
