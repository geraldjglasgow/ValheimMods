using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A plain byte buffer that records are written into (<see cref="RecordWriter"/>), in the bytes BinaryWriter (a
    /// ZPackage's writer) would write: little-endian numbers, a float's own four bytes, a string as its UTF-8 length in
    /// the 7-bit form and its UTF-8 bytes (BinaryWriter's own encoding: no byte order mark, an invalid string throws),
    /// and ZPackage.WriteNumItems' item counts. It grows by doubling and is reused, so nothing is allocated once it is
    /// big enough; one per thread that writes (each lane of <see cref="PrepPool"/> has its own).
    /// </summary>
    internal sealed class RecordBuffer
    {
        private const int StartSize = 64 * 1024;

        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        [StructLayout(LayoutKind.Explicit)]
        private struct Bits
        {
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public int Int;
        }

        public byte[] Bytes = new byte[StartSize];
        public int Size;

        public void Clear() => Size = 0;

        public void U8(byte value)
        {
            Room(1);
            Bytes[Size++] = value;
        }

        public void U16(ushort value)
        {
            Room(2);
            Bytes[Size] = (byte)value;
            Bytes[Size + 1] = (byte)(value >> 8);
            Size += 2;
        }

        public void I32(int value)
        {
            Room(4);
            Put(Size, value);
            Size += 4;
        }

        /// <summary>Writes over four bytes already written (a length put in once it is known).</summary>
        public void Put(int at, int value)
        {
            Bytes[at] = (byte)value;
            Bytes[at + 1] = (byte)(value >> 8);
            Bytes[at + 2] = (byte)(value >> 16);
            Bytes[at + 3] = (byte)(value >> 24);
        }

        public void I64(long value)
        {
            I32((int)value);
            I32((int)(value >> 32));
        }

        public void F32(float value) => I32(new Bits { Float = value }.Int);

        public void Vec3(Vector3 value)
        {
            F32(value.x);
            F32(value.y);
            F32(value.z);
        }

        public void Quat(Quaternion value)
        {
            F32(value.x);
            F32(value.y);
            F32(value.z);
            F32(value.w);
        }

        /// <summary>An object id as ZPackage.Write(ZDOID) writes it: the user id, then the number.</summary>
        public void Id(ZDOID id)
        {
            I64(id.UserID);
            I32((int)id.ID);
        }

        /// <summary>ZPackage.WriteNumItems: one byte under 128, else two with the top bit set.</summary>
        public void Count(int count)
        {
            if (count < 128)
            {
                U8((byte)count);
                return;
            }
            U8((byte)((count >> 8) | 0x80));
            U8((byte)count);
        }

        /// <summary>BinaryWriter.Write(string): the UTF-8 length in the 7-bit form, then the bytes; throws as it does.</summary>
        public void Text(string value)
        {
            int length = Utf8.GetByteCount(value);
            Varint(length);
            Room(length);
            Size += Utf8.GetBytes(value, 0, value.Length, Bytes, Size);
        }

        /// <summary>ZPackage.Write(byte[]): the length as an int, then the bytes.</summary>
        public void ByteArray(byte[] value)
        {
            I32(value.Length);
            Run(value, 0, value.Length);
        }

        public void Run(byte[] source, int at, int length)
        {
            Room(length);
            Buffer.BlockCopy(source, at, Bytes, Size, length);
            Size += length;
        }

        /// <summary>BinaryWriter.Write7BitEncodedInt.</summary>
        public void Varint(int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                U8((byte)(v | 0x80));
                v >>= 7;
            }
            U8((byte)v);
        }

        private void Room(int more)
        {
            if (Size + more <= Bytes.Length)
            {
                return;
            }
            int size = Bytes.Length;
            while (size < Size + more)
            {
                size *= 2;
            }
            System.Array.Resize(ref Bytes, size);
        }
    }
}
