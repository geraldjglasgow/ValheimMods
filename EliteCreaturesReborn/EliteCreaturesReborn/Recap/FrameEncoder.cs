using System;
using System.Collections.Generic;
using System.Threading;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Recap
{
    /// <summary>
    /// Turns raw screen pictures into JPEG frames on a background thread of its own, so the game's frame never waits on
    /// the encoder, then hands each frame to the <see cref="FrameRing"/>. Pixel buffers are reused through a small pool;
    /// a buffer of another size (the video height or the screen changed) is dropped instead.
    /// </summary>
    internal static class FrameEncoder
    {
        private sealed class Job
        {
            public byte[] Pixels = null!;
            public int Width;
            public int Height;
            public float Time;
            public float Keep;
        }

        private static readonly object _lock = new object();
        private static readonly Queue<Job> _jobs = new Queue<Job>();
        private static readonly Stack<byte[]> _pool = new Stack<byte[]>();
        private static Thread? _thread;
        private static bool _busy;

        /// <summary>Encoding failed once; recording stops for the session rather than failing fifteen times a second.</summary>
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

        /// <summary>A pixel buffer of exactly <paramref name="size"/> bytes, from the pool when one fits.</summary>
        public static byte[] Rent(int size)
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    byte[] buffer = _pool.Pop();
                    if (buffer.Length == size)
                    {
                        return buffer;
                    }
                }
            }
            return new byte[size];
        }

        public static void Submit(byte[] pixels, int width, int height, float time, float keep)
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
                    byte[]? jpeg = JpegCodec.Encode(job.Pixels, job.Width, job.Height);
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
                Done(job);
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

        private static void Done(Job job)
        {
            lock (_lock)
            {
                _busy = false;
                if (_pool.Count < 4)
                {
                    _pool.Push(job.Pixels);
                }
            }
        }
    }
}
