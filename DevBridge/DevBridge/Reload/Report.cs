using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Reload
{
    /// <summary>What a teardown removed (counted per kind) and what it found but left in place (one line each).</summary>
    internal sealed class Report
    {
        private const int MaxLines = 40;

        internal readonly SortedDictionary<string, int> Removed = new SortedDictionary<string, int>();
        internal readonly List<string> Left = new List<string>();
        internal readonly List<string> Destroyed = new List<string>();
        internal readonly List<string> Commands = new List<string>();

        internal void Count(string kind, int count = 1)
        {
            if (count <= 0) return;
            Removed.TryGetValue(kind, out int already);
            Removed[kind] = already + count;
        }

        internal void Leave(string line)
        {
            if (Left.Count < MaxLines && !Left.Contains(line)) Left.Add(line);
        }

        /// <summary>"3 x Name" for each distinct name, most frequent first, at most max of them.</summary>
        internal static string Tally(IEnumerable<string> names, int max = 8)
        {
            var groups = names.GroupBy(n => n).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).ToList();
            string text = string.Join(", ", groups.Take(max).Select(g => g.Count() == 1 ? g.Key : $"{g.Count()} x {g.Key}"));
            return groups.Count > max ? text + $", and {groups.Count - max} more" : text;
        }
    }
}
