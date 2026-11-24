using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace DevBridge.Perf
{
    /// <summary>
    /// One file of a recording, filled on the main thread in chunks a RecordWriter puts on disk. A full chunk (or a part
    /// one at a flush) is handed over and a free one taken back, so the main thread only copies bytes. When the writer
    /// falls behind by more than MaxChunks, Room says no and the caller marks what it could not write.
    /// </summary>
    internal sealed class RecordStream
    {
        private const int MaxChunks = 64;

        private readonly RecordWriter writer;
        private readonly int chunkBytes;
        private readonly Stack<byte[]> free = new Stack<byte[]>();
        private byte[] chunk;
        private int used;
        private int made;

        internal readonly FileStream File;
        internal long Bytes;

        internal RecordStream(RecordWriter writer, string path, int chunkBytes)
        {
            this.writer = writer;
            this.chunkBytes = chunkBytes;
            File = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 16);
            chunk = New();
        }

        /// <summary>True when bytes more fit, handing the chunk over first if it is full; false while the writer is behind.</summary>
        internal bool Room(int bytes)
        {
            if (chunk != null && used + bytes <= chunk.Length) return true;
            Flush();
            return chunk != null;
        }

        /// <summary>Hands what the chunk holds to the writer, so the file on disk is current; takes a free chunk back.</summary>
        internal void Flush()
        {
            if (chunk != null && used > 0) writer.Hand(this, chunk, used);
            if (chunk != null && used == 0) return;
            Bytes += used;
            used = 0;
            chunk = Take();
        }

        /// <summary>Called by the writer thread once a chunk is on disk.</summary>
        internal void Return(byte[] written)
        {
            lock (free) free.Push(written);
        }

        private byte[] Take()
        {
            lock (free)
            {
                if (free.Count > 0) return free.Pop();
            }
            return made < MaxChunks ? New() : null;
        }

        private byte[] New()
        {
            made++;
            return new byte[chunkBytes];
        }

        internal void U1(byte value) => chunk[used++] = value;

        internal void U2(int value)
        {
            chunk[used++] = (byte)value;
            chunk[used++] = (byte)(value >> 8);
        }

        internal void U4(uint value)
        {
            chunk[used++] = (byte)value;
            chunk[used++] = (byte)(value >> 8);
            chunk[used++] = (byte)(value >> 16);
            chunk[used++] = (byte)(value >> 24);
        }

        internal void U4(long value) => U4(value < 0 ? 0u : value > uint.MaxValue ? uint.MaxValue : (uint)value);

        internal void F4(float value) => U4(new Bits { F = value }.U);

        internal void F8(double value)
        {
            var bits = new Bits { D = value };
            U4(bits.U);
            U4(bits.High);
        }

        /// <summary>A float's or double's bytes without BitConverter's allocation (SingleToInt32Bits is not in this framework).</summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Bits
        {
            [FieldOffset(0)] internal float F;
            [FieldOffset(0)] internal double D;
            [FieldOffset(0)] internal uint U;
            [FieldOffset(4)] internal uint High;
        }
    }
}
