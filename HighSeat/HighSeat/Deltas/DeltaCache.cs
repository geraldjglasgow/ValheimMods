using System;
using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The delta records worked out in this record window, so the players who hold the same revisions of an object get
    /// the same bytes without the diff being run again: in a crowd every player sent an object each round holds the
    /// round before's record, so one diff serves them all, and one each for those sent it every 2nd or 4th round. Keyed
    /// by the object and the revisions held; an entry is used only for the very record it was made from (its place in
    /// the record cache, <see cref="Records"/>, which is unique until the cache is emptied), and is either the delta's
    /// bytes or "the whole record is smaller". Emptied with the record cache (<see cref="Records.NewFrame"/>, every
    /// 0.25 s), so every entry points at a record that still exists; the buffer and the table are reused. Written only on
    /// the main thread, outside a batch's preparation, so the send threads may look entries up then (<see cref="Ahead"/>).
    /// </summary>
    internal static class DeltaCache
    {
        private struct Key : IEquatable<Key>
        {
            public ZDOID Id;
            public uint Data;
            public ushort Owner;

            public bool Equals(Key other) => Id == other.Id && Data == other.Data && Owner == other.Owner;

            public override bool Equals(object other) => other is Key key && Equals(key);

            public override int GetHashCode() => Id.GetHashCode() ^ (int)(Data * 16777619u) ^ Owner << 16;
        }

        private struct Entry
        {
            public int Record;
            public int At;
            public int Length;
        }

        private static readonly Dictionary<Key, Entry> Made = new Dictionary<Key, Entry>();

        /// <summary>Where new delta records are appended; read back from <see cref="Bytes"/>.</summary>
        public static readonly RecordBuffer Buffer = new RecordBuffer();

        public static byte[] Bytes => Buffer.Bytes;

        /// <summary>
        /// The answer worked out before for a player holding these revisions and the record at <paramref name="record"/>:
        /// the delta's place in <see cref="Bytes"/>, or a length of -1 when the whole record goes.
        /// </summary>
        public static bool Find(ZDOID id, uint data, ushort owner, int record, out int at, out int length)
        {
            at = 0;
            length = -1;
            if (!Made.TryGetValue(new Key { Id = id, Data = data, Owner = owner }, out Entry entry) || entry.Record != record)
            {
                return false;
            }
            at = entry.At;
            length = entry.Length;
            return true;
        }

        public static void Store(ZDOID id, uint data, ushort owner, int record, int at, int length)
        {
            Made[new Key { Id = id, Data = data, Owner = owner }] = new Entry { Record = record, At = at, Length = length };
        }

        public static void Clear()
        {
            if (Made.Count > 0)
            {
                Made.Clear();
            }
            Buffer.Clear();
        }
    }
}
