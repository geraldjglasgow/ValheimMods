using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DevBridge.Logs
{
    /// <summary>
    /// A thread-safe ring of numbered lines, so a caller can ask for everything after the last number it saw. A line
    /// keeps its parts as they came (level, source, text) and is put together only when read: most lines are never read.
    /// </summary>
    internal sealed class LineBuffer
    {
        internal struct Line
        {
            internal long Seq;
            internal LogLevel Level;
            internal string Source;
            internal string Body;
            internal bool ShowLevel;

            /// <summary>The line as read: "Warning OpenKeep: text" in the log, "console: text" in the console's.</summary>
            internal string Text => ShowLevel ? $"{Level} {Source}: {Body}" : Source + ": " + Body;
        }

        private readonly Line[] ring;
        private readonly bool showLevel;
        private readonly object gate = new object();
        private long next = 1;

        /// <summary>showLevel: each line's text starts with its level (the log), or only with its source (the console).</summary>
        internal LineBuffer(int capacity, bool showLevel)
        {
            ring = new Line[capacity];
            this.showLevel = showLevel;
        }

        /// <summary>The number the next line will get; pass it back as since= to read only newer lines.</summary>
        internal long Next
        {
            get { lock (gate) return next; }
        }

        internal void Add(LogLevel level, string source, string body)
        {
            lock (gate)
            {
                ring[next % ring.Length] = new Line { Seq = next, Level = level, Source = source, Body = body, ShowLevel = showLevel };
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
