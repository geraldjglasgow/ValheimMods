using System.Collections.Generic;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Takes top-level keys out of a rule file's text line by line, so every other line and comment stays as written:
    /// the key's line, the indented lines under it and the comment lines right above it. The first block taken out
    /// leaves a one-line note in its place; runs of blank lines left behind shrink to one.
    /// </summary>
    internal static class RuleTextStrip
    {
        public static string Strip(string text, string[] keys, string note)
        {
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            List<string> lines = new List<string>(text.Replace("\r\n", "\n").Split('\n'));
            bool noted = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!IsKeyLine(lines[i], keys))
                {
                    continue;
                }
                int start = CommentsAbove(lines, i, note);
                lines.RemoveRange(start, BlockEnd(lines, i) - start);
                i = start - 1;
                if (!noted)
                {
                    lines.Insert(start, note);
                    noted = true;
                    i = start;
                }
            }
            return string.Join(newline, CollapseBlanks(lines));
        }

        private static bool IsKeyLine(string line, string[] keys)
        {
            foreach (string key in keys)
            {
                if (line.StartsWith(key) && line.Substring(key.Length).TrimStart(' ', '\t').StartsWith(":"))
                {
                    return true;
                }
            }
            return false;
        }

        // The comment lines directly above a key describe it; a blank line or the note ends them.
        private static int CommentsAbove(List<string> lines, int keyLine, string note)
        {
            int start = keyLine;
            while (start > 0 && lines[start - 1].StartsWith("#") && lines[start - 1] != note)
            {
                start--;
            }
            return start;
        }

        // Indented lines belong to the key; blank and comment lines between them too, never the ones after its last.
        private static int BlockEnd(List<string> lines, int keyLine)
        {
            int end = keyLine + 1;
            for (int j = keyLine + 1; j < lines.Count; j++)
            {
                string trimmed = lines[j].Trim();
                if (trimmed.Length > 0 && (lines[j][0] == ' ' || lines[j][0] == '\t'))
                {
                    end = j + 1;
                }
                else if (trimmed.Length > 0 && !trimmed.StartsWith("#"))
                {
                    break;
                }
            }
            return end;
        }

        private static List<string> CollapseBlanks(List<string> lines)
        {
            List<string> kept = new List<string>(lines.Count);
            foreach (string line in lines)
            {
                bool blank = line.Trim().Length == 0;
                if (blank && kept.Count > 0 && kept[kept.Count - 1].Trim().Length == 0)
                {
                    continue;
                }
                kept.Add(line);
            }
            return kept;
        }
    }
}
