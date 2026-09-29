using System;
using System.Linq;

namespace EliteCreaturesPack.Core
{
    /// <summary>A setting that lists names separated by commas, like "TreasureChest_forestcrypt, TreasureChest_sunkencrypt".</summary>
    internal static class NameList
    {
        /// <summary>Whether the list names <paramref name="name"/> exactly (spaces around each entry ignored).</summary>
        public static bool Contains(string list, string name) =>
            list.Split(',').Any(entry => string.Equals(entry.Trim(), name, StringComparison.Ordinal));
    }
}
