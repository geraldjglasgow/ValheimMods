using System;
using System.Collections.Generic;
using System.Threading;
using EliteCreaturesReborn.Util;
using Unity.Collections;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// Turns raw screen pictures into JPEG frames on a background thread of its own, so the game's frame never waits on
    /// the encoder, then hands each frame to the <see cref="FrameRing"/>. The GPU reads each picture straight into a
    /// native buffer from a small pool (<see cref="Rent"/>); this thread copies it into its own array, gives the buffer
    /// back and encodes, so the game's thread never copies a picture. A buffer of another size (the video height or the
    /// screen changed) is freed on the game's thread as it next rents one. The pool never holds more buffers than were
    /// ever in use at once: those the GPU is filling and those waiting here.
    /// </summary>
    internal static class FrameEncoder
    {
        private sealed class Job
        {
            public NativeArray<byte> Pixels;
            public int Width;
            public int Height;
            public float Time;
            public float Keep;
        }

        private static readonly object _lock = new object();
        private static readonly Queue<Job> _jobs = new Queue<Job>();
        private static readonly Stack<NativeArray<byte>> _pool = new Stack<NativeArray<byte>>();
        private static Thread? _thread;
        private static bool _busy;

        /// <summary>The encoder thread's own copy of the picture it is encoding, reused while the size stays the same.</summary>
        private static byte[] _picture = Array.Empty<byte>();

        /// <summary>Encoding failed once; recording stops for the session rather than failing on every recorded frame.</summary>
        public static volatile bool Failed;

        /// <summary>Pictures waiting or being encoded; a recap waits for these before it takes its frames.</summary>
        public static int Pending
        {
            get
            {
                lock (_lock)
                {
                    return _jobs.Count + (_busy ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// The game's thread: a native buffer of exactly <paramref name="size"/> bytes for the GPU to read a picture into,
        /// from the pool when one fits; any of another size found on the way is freed.
        /// </summary>
        public static NativeArray<byte> Rent(int size)
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    NativeArray<byte> buffer = _pool.Pop();
                    if (buffer.Length == size)
                    {
                        return buffer;
                    }
                    buffer.Dispose(); // no read is filling it: it came back to the pool
                }
            }
            return new NativeArray<byte>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>A rented buffer back to the pool, once nothing reads into it or out of it any more.</summary>
        public static void Return(NativeArray<byte> buffer)
        {
            if (!buffer.IsCreated)
            {
                return;
            }
            lock (_lock)
            {
                _pool.Push(buffer);
            }
        }

        public static void Submit(NativeArray<byte> pixels, int width, int height, float time, float keep)
        {
            lock (_lock)
            {
                _jobs.Enqueue(new Job { Pixels = pixels, Width = width, Height = height, Time = time, Keep = keep });
                Monitor.Pulse(_lock);
            }
            EnsureThread();
        }

        private static void EnsureThread()
        {
            if (_thread != null)
            {
                return;
            }
            _thread = new Thread(Run) { IsBackground = true, Name = "ECR death recap encoder" };
            _thread.Start();
        }

        private static void Run()
        {
            while (true)
            {
                Job job = Next();
                try
                {
                    byte[]? jpeg = JpegCodec.Encode(CopyOut(job), job.Width, job.Height);
                    if (jpeg == null || jpeg.Length == 0)
                    {
                        throw new InvalidOperationException("the encoder returned no picture");
                    }
                    FrameRing.Add(new Frame { Time = job.Time, Jpeg = jpeg }, job.Keep);
                }
                catch (Exception e)
                {
                    Fail(e);
                }
                Done();
            }
        }

        // The picture into this thread's own array, and the native buffer straight back to the pool for the next read.
        private static byte[] CopyOut(Job job)
        {
            try
            {
                if (_picture.Length != job.Pixels.Length)
                {
                    _picture = new byte[job.Pixels.Length];
                }
                job.Pixels.CopyTo(_picture);
                return _picture;
            }
            finally
            {
                Return(job.Pixels);
            }
        }

        private static void Fail(Exception e)
        {
            if (!Failed)
            {
                Failed = true;
                Log.Warn("Death recap: frames cannot be encoded on this machine, so deaths are kept without video ("
                    + e.Message + ").");
            }
        }

        private static Job Next()
        {
            lock (_lock)
            {
                while (_jobs.Count == 0)
                {
                    Monitor.Wait(_lock);
                }
                _busy = true;
                return _jobs.Dequeue();
            }
        }

        private static void Done()
        {
            lock (_lock)
            {
                _busy = false;
            }
        }
    }
}
