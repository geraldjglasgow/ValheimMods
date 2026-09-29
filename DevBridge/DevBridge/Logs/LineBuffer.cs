using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DevBridge.Logs
{
    /// <summary>A thread-safe ring of numbered lines, so a caller can ask for everything after the last number it saw.</summary>
    internal sealed class LineBuffer
    {
        internal struct Line
        {
            internal long Seq;
            internal LogLevel Level;
            internal string Text;
        }

        private readonly Line[] ring;
        private readonly object gate = new object();
        private long next = 1;

        internal LineBuffer(int capacity)
        {
            ring = new Line[capacity];
        }

        /// <summary>The number the next line will get; pass it back as since= to read only newer lines.</summary>
        internal long Next
        {
            get { lock (gate) return next; }
        }

        internal void Add(string text, LogLevel level)
        {
            lock (gate)
            {
                ring[next % ring.Length] = new Line { Seq = next, Level = level, Text = text };
                next++;
            }
        }

        /// <summary>Kept lines numbered since or later, at most the newest limit of them.</summary>
        internal List<Line> Since(long since, int limit, Func<Line, bool> keep)
        {
            var result = new List<Line>();
            lock (gate)
            {
                for (long seq = Math.Max(since, Math.Max(1, next - ring.Length)); seq < next; seq++)
                {
                    Line line = ring[seq % ring.Length];
                    if (keep(line)) result.Add(line);
                }
            }
            if (result.Count > limit) result.RemoveRange(0, result.Count - limit);
            return result;
        }
    }
}
