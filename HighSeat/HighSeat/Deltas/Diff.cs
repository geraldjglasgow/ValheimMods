using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// What changed between two records of one object, written as data in ZDO.Serialize's own layout that holds only
    /// what a player who has the older record needs, so ZDO.Deserialize on their game gives exactly what the newer
    /// record would. Deserialize sets the persistent, distant and type flags and the prefab every time, the rotation and
    /// the connection only when flagged, and every list entry it reads by key (ZDOExtraData.Add); it never removes
    /// anything. So: the flags and prefab are always written (the newer record's); the rotation and the connection only
    /// when the newer record has them and they differ from the older one's (or it had none); and of each list only the
    /// entries whose key the older record lacks or whose value bytes differ. A key the newer record no longer has is
    /// left out, as the game's own full update leaves it on the player's game too. Lists are walked side by side, as
    /// both are written in key order (BinarySearchDictionary keeps its keys sorted); out of order, an entry is only ever
    /// sent when it need not be, never left out when it should go. Floats are compared as bytes, so -0 and 0 count as a
    /// change. Nothing is allocated: the two records are read in place and the changed entries noted as offsets. One per
    /// thread that works deltas out (<see cref="DeltaRecord"/>): it keeps its place in both records in its fields.
    /// </summary>
    internal sealed class Diff
    {
        private readonly DataSections _old = new DataSections();
        private readonly DataSections _new = new DataSections();
        private readonly List<int> _changed = new List<int>();
        private int _oldIndex;
        private int _oldAt;

        /// <summary>
        /// The changes from one data block to another, into <paramref name="into"/> (emptied first); false when either
        /// cannot be read.
        /// </summary>
        public bool Write(byte[] old, int oldAt, int oldLength, byte[] now, int nowAt, int nowLength, RecordBuffer into)
        {
            if (!_old.Read(old, oldAt, oldLength) || !_new.Read(now, nowAt, nowLength))
            {
                return false;
            }
            into.Clear();
            into.U16(0);
            into.Run(now, _new.PrefabAt, 4);
            ushort flags = (ushort)(_new.Flags & DataSections.KeptFlags);
            flags |= Part(into, _new.RotationAt, _old.RotationAt, DataSections.RotationSize, DataSections.RotationFlag);
            flags |= Part(into, _new.ConnectionAt, _old.ConnectionAt, DataSections.ConnectionSize, DataSections.ConnectionFlag);
            for (int type = 0; type < RecordBytes.Types; type++)
            {
                flags |= List(into, type);
            }
            into.Bytes[0] = (byte)flags;
            into.Bytes[1] = (byte)(flags >> 8);
            return true;
        }

        // A fixed part (rotation, connection) when the newer record has it and the older lacks it or has other bytes.
        private ushort Part(RecordBuffer into, int nowAt, int oldAt, int size, ushort flag)
        {
            if (nowAt < 0 || oldAt >= 0 && RecordBytes.Same(_old.Bytes, oldAt, _new.Bytes, nowAt, size))
            {
                return 0;
            }
            into.Run(_new.Bytes, nowAt, size);
            return flag;
        }

        // One list's changed entries, as its item count and the entries; its flag when there are any. A list whose bytes
        // are the same as the older one's as a whole (most of them, between two updates) is not walked.
        private ushort List(RecordBuffer into, int type)
        {
            if (Unchanged(type))
            {
                return 0;
            }
            Walk(type);
            if (_changed.Count == 0)
            {
                return 0;
            }
            into.Count(_changed.Count / 2);
            for (int i = 0; i < _changed.Count; i += 2)
            {
                into.Run(_new.Bytes, _changed[i], _changed[i + 1]);
            }
            return DataSections.ListFlag(type);
        }

        private bool Unchanged(int type)
        {
            int length = _new.ListEnd[type] - _new.ListAt[type];
            return _new.ListCount[type] == _old.ListCount[type] && length == _old.ListEnd[type] - _old.ListAt[type]
                && RecordBytes.Same(_old.Bytes, _old.ListAt[type], _new.Bytes, _new.ListAt[type], length);
        }

        // Notes where each entry of the newer list that the older list lacks or holds with other bytes lies.
        private void Walk(int type)
        {
            _changed.Clear();
            _oldIndex = 0;
            _oldAt = _old.ListAt[type];
            int stride = RecordBytes.Stride(type);
            int at = _new.ListAt[type];
            for (int i = 0; i < _new.ListCount[type]; i++)
            {
                int size = stride > 0 ? stride : RecordBytes.EntrySize(type, _new.Bytes, at, _new.End);
                if (!InOld(type, stride, at, size))
                {
                    _changed.Add(at);
                    _changed.Add(size);
                }
                at += size;
            }
        }

        // Whether the older list holds this entry with the same bytes; its cursor moves on to the first key not below it.
        private bool InOld(int type, int stride, int at, int size)
        {
            int key = RecordBytes.I32(_new.Bytes, at);
            int count = _old.ListCount[type];
            while (_oldIndex < count && RecordBytes.I32(_old.Bytes, _oldAt) < key)
            {
                _oldAt += stride > 0 ? stride : RecordBytes.EntrySize(type, _old.Bytes, _oldAt, _old.End);
                _oldIndex++;
            }
            if (_oldIndex >= count || RecordBytes.I32(_old.Bytes, _oldAt) != key)
            {
                return false;
            }
            int oldSize = stride > 0 ? stride : RecordBytes.EntrySize(type, _old.Bytes, _oldAt, _old.End);
            return oldSize == size && RecordBytes.Same(_old.Bytes, _oldAt, _new.Bytes, at, size);
        }
    }
}
