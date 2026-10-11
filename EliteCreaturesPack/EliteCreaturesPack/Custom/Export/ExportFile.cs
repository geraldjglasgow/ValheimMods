using System;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Files;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// Where an export goes and what it suggests calling the new creature. The file is
    /// <c>EliteCreaturesPack.Export.&lt;prefab&gt;.yml</c> in the BepInEx config folder of the machine that ran the command,
    /// replacing an earlier export of the same creature. Its name never matches the definition files' pattern
    /// (<see cref="CreatureFiles.Pattern"/>, <c>EliteCreaturesPack.Creatures*.yml</c>), so an export is never loaded by
    /// accident. The suggested name is one word the definition files accept that no prefab, custom creature or definition
    /// has yet.
    /// </summary>
    internal static class ExportFile
    {
        public const string Prefix = "EliteCreaturesPack.Export.";
        private const string Suffix = "_Custom";

        /// <summary>Writes the text (UTF-8, no byte order mark) and returns the file's full path.</summary>
        public static string Write(string creature, string text)
        {
            string path = Path.Combine(Paths.ConfigPath, Prefix + FileWord(creature) + ".yml");
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return path;
        }

        /// <summary><c>&lt;prefab&gt;_Custom</c>, then <c>_Custom2</c>, <c>_Custom3</c>... until one is free.</summary>
        public static string SuggestName(string prefab)
        {
            string stem = NameWord(prefab) + Suffix;
            string name = stem;
            for (int i = 2; Taken(name); i++)
            {
                name = stem + i;
            }
            return name;
        }

        private static bool Taken(string name) =>
            ZNetScene.instance.GetPrefab(name) != null
            || CustomPrefabs.TryGet(name, out _)
            || (CreatureFiles.Current?.Creatures.Any(definition =>
                string.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase)) ?? false);

        /// <summary>The prefab name as one word of the definition files (letters, digits, <c>_ - .</c>).</summary>
        private static string NameWord(string prefab) =>
            new string(prefab.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' ? c : '_').ToArray());

        private static string FileWord(string creature) =>
            new string(creature.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
    }
}
