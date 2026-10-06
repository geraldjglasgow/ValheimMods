using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;

namespace DevBridge.Heap
{
    /// <summary>
    /// Named censuses kept for a later diff (mark=a, then diff=a): what grew or shrank since, row by row. A row is a
    /// name and its figures in the census's own columns (objects: count and bytes; statics: items and nested items).
    /// </summary>
    internal static class HeapMarks
    {
        private static readonly Dictionary<string, Dictionary<string, long[]>> marks = new Dictionary<string, Dictionary<string, long[]>>();

        internal static void Save(string kind, string name, Dictionary<string, long[]> rows) => marks[kind + ":" + name] = rows;

        /// <summary>The rows that changed since the mark, the largest change (by weight) first.</summary>
        internal static List<Dictionary<string, object>> Diff(string kind, string name, Dictionary<string, long[]> now,
            string[] columns, Func<long[], long> weight, int top)
        {
            if (!marks.TryGetValue(kind + ":" + name, out Dictionary<string, long[]> then))
                throw new BridgeException($"no {kind} mark named '{name}': take one first with mark={name}");
            return now.Keys.Union(then.Keys)
                .Select(key => (key, change: Change(Row(then, key), Row(now, key)), now: Row(now, key)))
                .Where(row => row.change.Any(value => value != 0))
                .OrderByDescending(row => Math.Abs(weight(row.change)))
                .Take(top)
                .Select(row => Describe(row.key, row.now, row.change, columns))
                .ToList();
        }

        private static long[] Row(Dictionary<string, long[]> rows, string key) =>
            rows.TryGetValue(key, out long[] row) ? row : new long[2];

        private static long[] Change(long[] then, long[] now) => now.Select((value, i) => value - then[i]).ToArray();

        private static Dictionary<string, object> Describe(string key, long[] now, long[] change, string[] columns)
        {
            var row = new Dictionary<string, object> { ["name"] = key };
            for (int i = 0; i < columns.Length; i++)
            {
                row[columns[i]] = now[i];
                row[columns[i] + "_change"] = change[i];
            }
            return row;
        }
    }
}
