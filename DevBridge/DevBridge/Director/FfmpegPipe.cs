using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace DevBridge.Director
{
    /// <summary>
    /// Raw RGBA frames written to ffmpeg's standard input on a background thread. Frame buffers are pooled, and the
    /// queue holds a few frames, so a slow encoder makes the game wait instead of filling the memory.
    /// </summary>
    internal sealed class FfmpegPipe
    {
        private readonly Process process;
        private readonly Stream input;
        private readonly BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>(4);
        private readonly ConcurrentBag<byte[]> pool = new ConcurrentBag<byte[]>();
        private readonly StringBuilder errors = new StringBuilder();
        private readonly Thread writer;
        private readonly int frameBytes;

        internal string Command { get; }
        internal string Failure { get; private set; }

        internal FfmpegPipe(string ffmpeg, string arguments, int frameBytes)
        {
            this.frameBytes = frameBytes;
            Command = $"\"{ffmpeg}\" {arguments}";
            var info = new ProcessStartInfo(ffmpeg, arguments)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true,
            };
            process = Process.Start(info) ?? throw new IOException("ffmpeg did not start");
            process.ErrorDataReceived += (sender, line) => Keep(line.Data);
            process.BeginErrorReadLine();
            input = process.StandardInput.BaseStream;
            writer = new Thread(Write) { IsBackground = true, Name = "DevBridge recorder" };
            writer.Start();
        }

        internal byte[] Rent() => pool.TryTake(out byte[] buffer) ? buffer : new byte[frameBytes];

        /// <summary>Queues one frame; waits while the queue is full. A failed encoder drops the frame.</summary>
        internal void Push(byte[] frame)
        {
            if (Failure != null || queue.IsAddingCompleted) pool.Add(frame);
            else queue.Add(frame);
        }

        private void Write()
        {
            foreach (byte[] frame in queue.GetConsumingEnumerable())
            {
                try
                {
                    if (Failure == null) input.Write(frame, 0, frame.Length);
                }
                catch (IOException error)
                {
                    Failure = "ffmpeg stopped taking frames: " + error.Message;
                }
                pool.Add(frame);
            }
        }

        /// <summary>Closes the input and waits for ffmpeg to finish the file; returns an error or null.</summary>
        internal string Finish(int timeoutMs)
        {
            queue.CompleteAdding();
            writer.Join();
            try { input.Close(); } catch (IOException) { }
            if (!process.WaitForExit(timeoutMs)) return "ffmpeg did not finish in time";
            process.WaitForExit();
            if (process.ExitCode == 0) return Failure;
            lock (errors) return $"ffmpeg exited with code {process.ExitCode}: {Fmt.Clip(errors.ToString().Trim(), 800)}";
        }

        private void Keep(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            lock (errors)
            {
                if (errors.Length < 4000) errors.AppendLine(line);
            }
        }
    }
}
