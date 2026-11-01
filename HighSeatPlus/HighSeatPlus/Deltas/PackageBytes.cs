namespace HighSeatPlus
{
    /// <summary>
    /// Reads a package's bytes in place, as ZPackage's BinaryReader would (little-endian numbers, 7-bit encoded
    /// lengths), without copying or allocating. Readers that can run past the end return -1.
    /// </summary>
    internal static class PackageBytes
    {
        public static ushort U16(byte[] b, int p) => (ushort)(b[p] | b[p + 1] << 8);

        public static int I32(byte[] b, int p) => b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24;

        public static uint U32(byte[] b, int p) => (uint)I32(b, p);

        public static long I64(byte[] b, int p) => (uint)I32(b, p) | (long)I32(b, p + 4) << 32;

        /// <summary>The ZDOID at p, made as ZPackage.ReadZDOID makes it.</summary>
        public static ZDOID Id(byte[] b, int p) => new ZDOID(I64(b, p), U32(b, p + 8));

        /// <summary>A 7-bit encoded number at p (BinaryReader.Read7BitEncodedInt); -1 when broken or past end.</summary>
        public static int Varint(byte[] b, ref int p, int end)
        {
            int value = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                if (p < 0 || p >= end)
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

        /// <summary>Where a part of <paramref name="size"/> bytes at p starts (p moves past it); -1 when it runs past end.</summary>
        public static int Take(ref int p, int size, int end)
        {
            if (p < 0 || (long)p + size > end)
            {
                p = -1;
                return -1;
            }
            int at = p;
            p += size;
            return at;
        }
    }
}
