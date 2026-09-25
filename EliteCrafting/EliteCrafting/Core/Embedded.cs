using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EliteCrafting.Core
{
    /// <summary>
    /// Reads the text resources compiled into the DLL: the built-in YAML defaults (<c>EliteCrafting.config.*</c>) and
    /// the English words (<c>EliteCrafting.translations.*</c>). Names are the csproj's LogicalName, never paths.
    /// </summary>
    public static class Embedded
    {
        private static readonly Assembly Self = typeof(Embedded).Assembly;

        /// <summary>The resource's text, or null when there is no such resource.</summary>
        public static string? Text(string logicalName)
        {
            using Stream? stream = Self.GetManifestResourceStream(logicalName);
            if (stream == null)
            {
                return null;
            }
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>Every resource name starting with the prefix and ending with the suffix, in ordinal order.</summary>
        public static List<string> Names(string prefix, string suffix)
        {
            List<string> names = new List<string>();
            foreach (string name in Self.GetManifestResourceNames())
            {
                if (name.StartsWith(prefix, System.StringComparison.Ordinal)
                    && name.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    names.Add(name);
                }
            }
            names.Sort(System.StringComparer.Ordinal);
            return names;
        }
    }
}
