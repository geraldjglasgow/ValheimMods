using System;
using System.Collections.Generic;
using System.Threading;

namespace DevBridge.Perf
{
    /// <summary>
    /// The one thread that puts a recording's chunks on disk, in the order they were handed over, so the main thread never
    /// waits on the disk. After a write fails it keeps taking chunks and drops them, and Error says why.
    /// </summary>
    internal sealed class RecordWriter
    {
        private struct Job
        {
            internal RecordStream Stream;
            internal byte[] Chunk;
            internal int Used;
        }

        private readonly Queue<Job> jobs = new Queue<Job>();
        private readonly object gate = new object();
        private readonly Thread thread;
        private bool closing;

        internal volatile string Error;

        internal RecordWriter()
        {
            thread = new Thread(Loop) { IsBackground = true, Name = "DevBridge perf writer" };
            thread.Start();
        }

        internal void Hand(RecordStream stream, byte[] chunk, int used)
        {
            lock (gate)
            {
                jobs.Enqueue(new Job { Stream = stream, Chunk = chunk, Used = used });
                Monitor.Pulse(gate);
            }
        }

        /// <summary>Writes what is still queued, then ends the thread; the caller closes the files after.</summary>
        internal void Close()
        {
            lock (gate)
            {
                closing = true;
                Monitor.Pulse(gate);
            }
            thread.Join();
        }

        private void Loop()
        {
            while (Next(out Job job)) Write(job);
        }

        private bool Next(out Job job)
        {
            lock (gate)
            {
                while (jobs.Count == 0 && !closing) Monitor.Wait(gate);
                if (jobs.Count == 0)
                {
                    job = default;
                    return false;
                }
                job = jobs.Dequeue();
                return true;
            }
        }

        private void Write(Job job)
        {
            try
            {
                if (Error != null) return;
                job.Stream.File.Write(job.Chunk, 0, job.Used);
                job.Stream.File.Flush();
            }
            catch (Exception error)
            {
                Error = $"{error.GetType().Name}: {error.Message}";
            }
            finally
            {
                job.Stream.Return(job.Chunk);
            }
        }
    }
}
