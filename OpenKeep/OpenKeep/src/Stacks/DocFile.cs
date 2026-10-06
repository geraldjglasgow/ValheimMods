using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenKeep.Stacks
{
    /// <summary>
    /// The documentation files (OpenKeep.Items.txt, OpenKeep.Containers.txt, OpenKeep.Stations.txt) are written only when
    /// their text changed: a world load asks for them several times (the item database, the scene, the stack rules
    /// arriving from the server), and nothing changes between most loads. The text last written is kept, else the file
    /// on disk is compared; a file that is gone is always written, and the console's <c>openkeep write docs</c> always
    /// writes.
    /// </summary>
    public static class DocFile
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        private static readonly Dictionary<string, string> written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Writes the text unless the file already holds it (or <paramref name="force"/>); true when written.</summary>
        public static bool Write(string path, string text, bool force)
        {
            if (!force && File.Exists(path) && Holds(path, text))
                return false;
            File.WriteAllText(path, text, Utf8);
            written[path] = text;
            return true;
        }

        private static bool Holds(string path, string text)
        {
            if (written.TryGetValue(path, out string last))
                return last == text;
            bool same = File.ReadAllText(path, Utf8) == text;
            if (same)
                written[path] = text;
            return same;
        }
    }
}
