using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Sync
{
    /// <summary>Compares two /eval results as text; an error is a result like any other ("error: ...").</summary>
    internal static class TextCompare
    {
        private const int MaxLines = 200;

        internal static Dictionary<string, object> Compare(PeerAnswer here, PeerAnswer there)
        {
            string mine = Result(here), theirs = Result(there);
            var row = new Dictionary<string, object> { ["same"] = mine == theirs };
            if (mine == theirs) return row;
            string[] myLines = Lines(mine), theirLines = Lines(theirs);
            if (myLines.Length <= 1 && theirLines.Length <= 1)
            {
                row["value"] = theirs;
                return row;
            }
            row["onlyHere"] = Missing(myLines, theirLines);
            row["onlyThere"] = Missing(theirLines, myLines);
            return row;
        }

        internal static string Result(PeerAnswer answer) => answer.Body.Replace("\r", "").TrimEnd('\n');

        private static string[] Lines(string text) => text.Split('\n');

        /// <summary>The lines of one side the other lacks, counting repeats, in their order.</summary>
        private static List<string> Missing(string[] from, string[] other)
        {
            Dictionary<string, int> left = other.GroupBy(line => line, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var missing = new List<string>();
            foreach (string line in from)
            {
                if (left.TryGetValue(line, out int count) && count > 0) left[line] = count - 1;
                else if (missing.Count < MaxLines) missing.Add(line);
            }
            return missing;
        }
    }
}
