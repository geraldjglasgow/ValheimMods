namespace HighSeat
{
    /// <summary>
    /// Where the parts of one object's data lie in a record's bytes, found in one pass over ZDO.Serialize's layout: the
    /// flags (a ushort: the seven list bits from 2 to 0x80 and connections 1, persistent 0x100, distant 0x200, the type in
    /// 0xC00, rotation 0x1000), the prefab, the rotation (12 bytes, when flagged), and under any of the low eight bits the
    /// connection (its type byte and target ZDOID, 13 bytes, when flagged) and each list (an item count, then each
    /// entry: its int key and its value). <see cref="Read"/> is false unless the parts end exactly where the data does,
    /// so a record this does not understand is never diffed. Two instances are reused, one per side of a diff.
    /// </summary>
    internal sealed class DataSections
    {
        public const ushort ConnectionFlag = 0x1;
        public const ushort LowBits = 0xFF;
        public const ushort KeptFlags = 0x100 | 0x200 | 0xC00;
        public const ushort RotationFlag = 0x1000;
        public const int RotationSize = 12;
        public const int ConnectionSize = 13;

        public byte[] Bytes = null!;
        public int End;
        public ushort Flags;
        public int PrefabAt;
        public int RotationAt;
        public int ConnectionAt;
        public readonly int[] ListAt = new int[RecordBytes.Types];
        public readonly int[] ListCount = new int[RecordBytes.Types];
        public readonly int[] ListEnd = new int[RecordBytes.Types];

        /// <summary>The list bit of a value type (ZDO.ExtraDataFlags: Floats 2 ... ByteArrays 0x80).</summary>
        public static ushort ListFlag(int type) => (ushort)(2 << type);

        /// <summary>Finds the parts of the data at <paramref name="at"/>, <paramref name="length"/> bytes long.</summary>
        public bool Read(byte[] bytes, int at, int length)
        {
            Bytes = bytes;
            int end = at + length;
            End = end;
            if (length < 6)
            {
                return false;
            }
            Flags = RecordBytes.U16(bytes, at);
            PrefabAt = at + 2;
            int p = at + 6;
            RotationAt = Take(ref p, RotationFlag, RotationSize, end);
            ConnectionAt = (Flags & LowBits) != 0 ? Take(ref p, ConnectionFlag, ConnectionSize, end) : -1;
            for (int type = 0; type < RecordBytes.Types && p >= 0; type++)
            {
                p = (Flags & LowBits) != 0 && (Flags & ListFlag(type)) != 0 ? List(type, p, end) : Empty(type, p);
                ListEnd[type] = p;
            }
            return p == end;
        }

        // A fixed part present under its flag: where it starts (p moves past it), else -1; p is -1 when it runs past end.
        private int Take(ref int p, ushort flag, int size, int end)
        {
            if (p < 0 || (Flags & flag) == 0)
            {
                return -1;
            }
            int start = p;
            p = p + size <= end ? p + size : -1;
            return start;
        }

        private int Empty(int type, int p)
        {
            ListAt[type] = p;
            ListCount[type] = 0;
            return p;
        }

        // One list: its count, then where it ends (fixed-size values by arithmetic, strings and byte arrays walked); -1 when
        // it runs past end.
        private int List(int type, int p, int end)
        {
            int count = RecordBytes.NumItems(Bytes, ref p, end);
            ListAt[type] = p;
            ListCount[type] = count;
            int stride = RecordBytes.Stride(type);
            if (stride > 0)
            {
                long last = p + (long)count * stride;
                return count < 0 || last > end ? -1 : (int)last;
            }
            for (int i = 0; i < count && p >= 0; i++)
            {
                int size = RecordBytes.EntrySize(type, Bytes, p, end);
                p = size < 0 ? -1 : p + size;
            }
            return count < 0 ? -1 : p;
        }
    }
}
