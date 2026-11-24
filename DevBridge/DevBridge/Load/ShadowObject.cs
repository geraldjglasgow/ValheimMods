using System.Collections.Generic;

namespace DevBridge.Load
{
    /// <summary>
    /// A verifying fake player's own copy of one object (<see cref="FakeShadow"/>), built only from what the server sent
    /// it, as a player's game would hold it: the revisions, owner and position of the last record it took, and the
    /// object's data laid down the way ZDO.Deserialize lays it down: the persistent, distant and type flags and the
    /// prefab every time, the rotation and the connection only when the record has them, and every list entry by its
    /// type and key, overwritten or added, never removed. Values are kept as their raw bytes (floats included), so a
    /// comparison is exact. <see cref="Read"/> parses a record's data the same way into a fresh copy, which is how the
    /// server's own record is read for the check (<see cref="ShadowVerify"/>).
    /// </summary>
    internal sealed class ShadowObject
    {
        private const ushort LowBits = 0xFF;
        private const ushort KeptFlags = 0x100 | 0x200 | 0xC00;
        private const ushort RotationFlag = 0x1000;
        private const int Types = 7;
        private static readonly int[] FixedSize = { 4, 12, 16, 4, 8, -1, -2 };

        internal uint Data;
        internal ushort Owner;
        internal long OwnerId;
        internal byte[] Position;
        internal ushort Flags;
        internal int Prefab;
        internal byte[] Rotation;
        internal byte[] Connection;
        internal readonly Dictionary<long, byte[]> Fields = new Dictionary<long, byte[]>();

        /// <summary>The key of a list entry: its value type (ZDOExtraData.Type order) and its hash.</summary>
        internal static long FieldKey(int type, int hash) => (long)type << 32 | (uint)hash;

        /// <summary>A record's data parsed into a fresh copy (no revisions, owner or position).</summary>
        internal static ShadowObject Read(byte[] data)
        {
            var copy = new ShadowObject();
            copy.Apply(data);
            return copy;
        }

        /// <summary>Lays a record's data over this copy, as ZDO.Deserialize does.</summary>
        internal void Apply(byte[] data)
        {
            var pkg = new ZPackage(data);
            ushort flags = pkg.ReadUShort();
            Flags = (ushort)(flags & KeptFlags);
            Prefab = pkg.ReadInt();
            if ((flags & RotationFlag) != 0) Rotation = pkg.ReadByteArray(12);
            if ((flags & LowBits) == 0) return;
            if ((flags & 1) != 0) Connection = pkg.ReadByteArray(13);
            for (int type = 0; type < Types; type++)
            {
                if ((flags & (2 << type)) != 0) List(pkg, type);
            }
        }

        private void List(ZPackage pkg, int type)
        {
            int count = pkg.ReadNumItems();
            for (int i = 0; i < count; i++)
            {
                int hash = pkg.ReadInt();
                Fields[FieldKey(type, hash)] = Value(pkg, type);
            }
        }

        // One value's raw bytes: fixed sizes, a string's 7-bit length and bytes, a byte array's int length and bytes.
        private static byte[] Value(ZPackage pkg, int type)
        {
            int size = FixedSize[type];
            if (size > 0) return pkg.ReadByteArray(size);
            int start = pkg.GetPos();
            int length = size == -1 ? Varint(pkg) : pkg.ReadInt();
            int prefix = pkg.GetPos() - start;
            pkg.SetPos(start);
            return pkg.ReadByteArray(prefix + length);
        }

        internal static int Varint(ZPackage pkg)
        {
            int value = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                byte part = pkg.ReadByte();
                value |= (part & 0x7F) << shift;
                if ((part & 0x80) == 0) return value;
            }
            throw new System.IO.InvalidDataException("a 7-bit number longer than 5 bytes");
        }
    }
}
