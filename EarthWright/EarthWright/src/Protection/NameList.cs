using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace EarthWright.Protection
{
    /// <summary>
    /// A setting holding names separated by commas or semicolons, as a case-insensitive set. The set is rebuilt only
    /// when the setting's text changes, so checks that run for every stroke (or every vertex) stay cheap.
    /// </summary>
    public sealed class NameList
    {
        private readonly ConfigEntry<string> entry;
        private string parsedFrom;
        private HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public NameList(ConfigEntry<string> entry)
        {
            this.entry = entry;
        }

        /// <summary>The current names. Do not modify.</summary>
        public HashSet<string> Names
        {
            get
            {
                string value = entry?.Value ?? "";
                if (!string.Equals(value, parsedFrom, StringComparison.Ordinal))
                {
                    names = Parse(value);
                    parsedFrom = value;
                }
                return names;
            }
        }

        /// <summary>Changes whenever the names are rebuilt, for caches derived from them.</summary>
        public string Source
        {
            get
            {
                _ = Names;
                return parsedFrom;
            }
        }

        public static HashSet<string> Parse(string value)
        {
            IEnumerable<string> parts = (value ?? "").Split(',', ';').Select(s => s.Trim()).Where(s => s.Length > 0);
            return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
        }
    }
}
