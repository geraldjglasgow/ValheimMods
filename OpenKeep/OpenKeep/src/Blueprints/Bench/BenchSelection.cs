using System.Collections.Generic;
using System.Linq;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The picked lines of one list, as in a file explorer: a click picks one, Ctrl + click adds or drops one, Shift +
    /// click picks the range from the last one clicked (Ctrl + Shift adds the range). The way up is never picked. Kept by
    /// line key, so a refill keeps what is still listed.
    /// </summary>
    public sealed class BenchSelection
    {
        private readonly HashSet<string> keys = new HashSet<string>();
        private string anchor;

        public int Count => keys.Count;

        /// <summary>Goes up with every change, so the view repaints only then.</summary>
        public int Version { get; private set; }

        public bool Has(BenchEntry entry) => keys.Contains(entry.Key);

        public List<BenchEntry> Of(List<BenchEntry> entries) => entries.Where(Has).ToList();

        public void Clear()
        {
            keys.Clear();
            anchor = null;
            Version++;
        }

        /// <summary>Picks this line alone (a drag starting on a line that was not picked).</summary>
        public void Only(BenchEntry entry)
        {
            keys.Clear();
            if (entry.Kind != BenchKind.Up)
                keys.Add(entry.Key);
            anchor = entry.Key;
            Version++;
        }

        public void Click(List<BenchEntry> entries, int index, bool ctrl, bool shift)
        {
            BenchEntry entry = entries[index];
            int from = anchor == null ? -1 : entries.FindIndex(e => e.Key == anchor);
            if (shift && from >= 0)
            {
                if (!ctrl)
                    keys.Clear();
                for (int i = System.Math.Min(from, index); i <= System.Math.Max(from, index); i++)
                {
                    if (entries[i].Kind != BenchKind.Up)
                        keys.Add(entries[i].Key);
                }
                Version++;
                return;
            }
            if (!ctrl)
                keys.Clear();
            if (entry.Kind != BenchKind.Up && !keys.Remove(entry.Key))
                keys.Add(entry.Key);
            anchor = entry.Key;
            Version++;
        }

        /// <summary>After a refill: lines no longer listed are dropped.</summary>
        public void Keep(List<BenchEntry> entries)
        {
            HashSet<string> listed = new HashSet<string>(entries.Select(e => e.Key));
            if (keys.RemoveWhere(k => !listed.Contains(k)) > 0)
                Version++;
            if (anchor != null && !listed.Contains(anchor))
                anchor = null;
        }
    }
}
