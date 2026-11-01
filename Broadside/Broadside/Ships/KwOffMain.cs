using System;
using System.Diagnostics;
using System.Threading;

namespace Broadside
{
    /// <summary>
    /// Plain work run off the main thread, on the thread pool, so a long cut never holds up a frame: a hull part's holes
    /// (<see cref="KwHolePart"/>, tens of milliseconds for a big hole in a big part) and a sail's holes in its texture
    /// (<see cref="KwSailHoling"/>). Started on the main thread, which takes the result there once it is done
    /// (<see cref="KwOffMainJob{T}"/>); a throw is kept and handed back there. At most <see cref="MostAtOnce"/> run at a
    /// time (<see cref="Free"/>), so the game's own threads keep their cores. The work touches only plain data made for it
    /// (or never changed while it runs), never a Unity object: the cuts are plain arithmetic on lists and arrays.
    /// </summary>
    public static class KwOffMain
    {
        public const int MostAtOnce = 2;
        private static int running;

        /// <summary>Whether another job may start now.</summary>
        public static bool Free => Volatile.Read(ref running) < MostAtOnce;

        /// <summary>The work started on the thread pool; its job, to be asked on the main thread whether it is done.</summary>
        public static KwOffMainJob<T> Start<T>(Func<T> work)
        {
            KwOffMainJob<T> job = new KwOffMainJob<T>(work);
            Interlocked.Increment(ref running);
            if (!ThreadPool.QueueUserWorkItem(Run<T>, job))
            {
                Interlocked.Decrement(ref running);
                job.Run();
            }
            return job;
        }

        private static void Run<T>(object state)
        {
            try
            {
                ((KwOffMainJob<T>)state).Run();
            }
            finally
            {
                Interlocked.Decrement(ref running);
            }
        }
    }

    /// <summary>One piece of work off the main thread (<see cref="KwOffMain"/>): done or not, its result or its throw, how long it took.</summary>
    public sealed class KwOffMainJob<T>
    {
        private readonly Func<T> work;
        private volatile bool done;

        internal KwOffMainJob(Func<T> work) => this.work = work;

        /// <summary>Whether it has finished (its result, or its throw, is ready).</summary>
        public bool Done => done;

        public T Result { get; private set; } = default!;
        public Exception? Error { get; private set; }

        /// <summary>How long the work took, in stopwatch ticks.</summary>
        public long Ticks { get; private set; }

        internal void Run()
        {
            long start = Stopwatch.GetTimestamp();
            try
            {
                Result = work();
            }
            catch (Exception e)
            {
                Error = e;
            }
            Ticks = Stopwatch.GetTimestamp() - start;
            done = true;
        }
    }
}
