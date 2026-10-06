using System;
using System.Collections.Generic;

namespace EliteCreaturesPack.Core
{
    /// <summary>A setting that lists names separated by commas, like "TreasureChest_forestcrypt, TreasureChest_sunkencrypt".</summary>
    internal static class NameList
    {
        private static string? parsedList;
        private static HashSet<string> parsed = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Whether the list names <paramref name="name"/> exactly (spaces around each entry ignored). The list is split
        /// once per value: a config entry hands back the same string until it is edited, so the last one parsed is kept.
        /// </summary>
        public static bool Contains(string list, string name)
        {
            if (!ReferenceEquals(list, parsedList))
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (string entry in list.Split(','))
                {
                    names.Add(entry.Trim());
                }
                parsed = names;
                parsedList = list;
            }
            return parsed.Contains(name);
        }
    }
}
