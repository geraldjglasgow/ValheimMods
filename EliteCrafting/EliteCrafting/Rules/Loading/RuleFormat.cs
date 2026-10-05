using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The YAML file format (classes-and-tiers.md section 7). Both families carry a root <c>format: 2</c> line; a text
    /// without it was written for format 1 (slots, a seven-tier window) and is never merged: an old main file on disk
    /// is renamed to <c>&lt;name&gt;.v1.bak</c> and the new default written in its place, an old extra file and any old
    /// text the server pushes are skipped, each with a warning. Read from the raw text, so a file with a syntax error
    /// elsewhere still has its format known.
    /// </summary>
    internal static class RuleFormat
    {
        public const string Key = "format";
        public const int Current = 2;

        private static readonly Regex Line = new Regex(@"^format[ \t]*:[ \t]*['""]?(\d+)['""]?[ \t]*(#[^\r\n]*)?\r?$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        /// <summary>The root <c>format:</c> value of a text, or 0 when it has none (format 1).</summary>
        public static int Of(string text)
        {
            Match match = Line.Match(text);
            return match.Success && Numbers.TryInt(match.Groups[1].Value, out int format) ? format : 0;
        }

        public static bool IsCurrent(string text) => Of(text) == Current;

        /// <summary>The texts in the current format; every other one is logged and left out.</summary>
        public static List<SourceText> KeepCurrent(IEnumerable<SourceText> files, string origin)
        {
            List<SourceText> kept = new List<SourceText>();
            foreach (SourceText file in files)
            {
                if (IsCurrent(file.Text))
                {
                    kept.Add(file);
                }
                else
                {
                    Log.Warn($"{file.Name} from {origin} is {Describe(file.Text)}: skipped (this version reads format {Current})");
                }
            }
            return kept;
        }

        /// <summary>"format 1 (an older EliteCrafting)" or "format 3 (a newer EliteCrafting)".</summary>
        public static string Describe(string text)
        {
            int format = Of(text);
            return format > Current ? $"format {format} (a newer EliteCrafting)" : $"format {Math.Max(format, 1)} (an older EliteCrafting)";
        }

        /// <summary>
        /// Renames an old main file to <c>&lt;name&gt;.v1.bak</c> (<c>.v1.2.bak</c> and up when taken) and writes the
        /// default text in its place. The new text, or null when the file could not be moved (it is then skipped).
        /// </summary>
        public static string? ReplaceOldMain(string path, string defaultText)
        {
            try
            {
                string backup = FreeBackupPath(path);
                File.Move(path, backup);
                File.WriteAllText(path, defaultText, new System.Text.UTF8Encoding(false));
                Log.Warn($"{Path.GetFileName(path)} was written for format 1 by an older EliteCrafting: renamed to "
                    + $"{Path.GetFileName(backup)} and the new default written in its place (copy your changes over by hand)");
                return defaultText;
            }
            catch (Exception e)
            {
                Log.Error($"{Path.GetFileName(path)} is format 1 and could not be replaced ({e.Message}): skipped");
                return null;
            }
        }

        private static string FreeBackupPath(string path)
        {
            string stem = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path));
            string candidate = stem + ".v1.bak";
            for (int n = 2; File.Exists(candidate); n++)
            {
                candidate = $"{stem}.v1.{n}.bak";
            }
            return candidate;
        }
    }
}
