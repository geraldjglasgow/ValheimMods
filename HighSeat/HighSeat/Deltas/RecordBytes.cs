namespace HighSeat
{
    /// <summary>
    /// Reads the bytes of a record in place, as BinaryReader (ZPackage's reader) would: little-endian numbers, the 7-bit
    /// lengths of strings (BinaryWriter.Write(string)), and the item counts of ZPackage.WriteNumItems (one byte under 128,
    /// else two with the top bit set). Nothing is copied and nothing allocated. A value's size in ZDO.Serialize's lists:
    /// floats 4, Vector3 12, Quaternion 16, ints 4, longs 8, strings their 7-bit length and bytes, byte arrays an int
    /// length and bytes. Every reader returns -1 (or false) for bytes that run past <c>end</c>, so a short or broken
    /// record is never read beyond its own bytes.
    /// </summary>
    internal static class RecordBytes
    {
        /// <summary>The seven value types in the order ZDO.Serialize writes them, as ZDOExtraData.Type.</summary>
        public const int Types = 7;

        private const int Strings = 5;
        private const int ByteArrays = 6;
        private static readonly int[] FixedSize = { 4, 12, 16, 4, 8, 0, 0 };

        public static ushort U16(byte[] b, int p) => (ushort)(b[p] | b[p + 1] << 8);

        public static int I32(byte[] b, int p) => b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24;

        public static uint U32(byte[] b, int p) => (uint)I32(b, p);

        public static long I64(byte[] b, int p) => (uint)I32(b, p) | (long)I32(b, p + 4) << 32;

        /// <summary>True when the two runs of bytes are the same.</summary>
        public static bool Same(byte[] a, int at, byte[] b, int bt, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a[at + i] != b[bt + i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>A 7-bit encoded number at p (BinaryReader.Read7BitEncodedInt); -1 when broken or past end.</summary>
        public static int Varint(byte[] b, ref int p, int end)
        {
            int value = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                if (p >= end)
                {
                    return -1;
                }
                byte part = b[p++];
                value |= (part & 0x7F) << shift;
                if ((part & 0x80) == 0)
                {
                    return value < 0 ? -1 : value;
                }
            }
            return -1;
        }

        /// <summary>ZPackage.ReadNumItems at p; -1 past end.</summary>
        public static int NumItems(byte[] b, ref int p, int end)
        {
            if (p >= end)
            {
                return -1;
            }
            int first = b[p++];
            if ((first & 0x80) == 0)
            {
                return first;
            }
            if (p >= end)
            {
                return -1;
            }
            return (first & 0x7F) << 8 | b[p++];
        }

        /// <summary>Bytes of one list entry at p (its int key, then its value of the type); -1 when it runs past end.</summary>
        public static int EntrySize(int type, byte[] b, int p, int end)
        {
            int value = p + 4;
            if (value > end)
            {
                return -1;
            }
            long size = FixedSize[type];
            if (type == Strings)
            {
                int at = value;
                int length = Varint(b, ref at, end);
                size = length < 0 ? -1 : at - value + (long)length;
            }
            else if (type == ByteArrays)
            {
                int length = value + 4 > end ? -1 : I32(b, value);
                size = length < 0 ? -1 : 4L + length;
            }
            return size < 0 || value + size > end ? -1 : 4 + (int)size;
        }

        /// <summary>Bytes of every entry of a list of fixed-size values (key and value); 0 for strings and byte arrays.</summary>
        public static int Stride(int type) => FixedSize[type] > 0 ? 4 + FixedSize[type] : 0;

        /// <summary>Bytes the 7-bit form of a number takes.</summary>
        public static int VarintSize(int value)
        {
            int size = 1;
            for (uint v = (uint)value; v >= 0x80; v >>= 7)
            {
                size++;
            }
            return size;
        }
    }
}
