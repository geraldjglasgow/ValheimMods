using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// Keeps every container prefab of the scene listed in OpenKeep.Containers.yml with its vanilla size, commented
    /// out, so the file itself is the list of prefab names. A file still equal to the embedded default gets the whole
    /// list in place of its examples; any other file gets the prefabs that no file of the set names yet (another mod's,
    /// installed later) appended at the end, so the player's own lines are never rewritten. Only on the author.
    /// </summary>
    public static class ContainerTemplate
    {
        private const string ContainersKey = "containers:";

        /// <summary>A key at the start of a line, commented out or not, quoted or not: the name before the colon.</summary>
        private static readonly Regex KeyLine = new Regex(@"^\s*(?:#\s*)?(['""]?)([^'""#\s][^:]*?)\1\s*:", RegexOptions.Compiled);

        public static void ListPrefabs()
        {
            SyncedConfiguration synced = CapacityModule.Synced;
            YamlFileSet set = CapacityModule.Set;
            if (synced == null || set == null || !synced.Yaml.IsAuthor || ZNetScene.instance == null)
                return;
            string path = MainFilePath(synced, set);
            if (!File.Exists(path))
                return;
            string text = File.ReadAllText(path);
            string template = DefaultText(set);
            bool isDefault = template != null && IsDefault(text, template, ContainersKey);
            string listed = isDefault ? Generate(template, ContainerPrefabs.All()) : AppendMissing(text, path, set, ContainerPrefabs.All());
            if (listed == null)
                return;
            Save(synced, set, path, listed);
            Plugin.Log.LogInfo(isDefault ? $"OpenKeep: listed every container prefab in {path}." : $"OpenKeep: listed new container prefabs in {path}.");
        }

        /// <summary>The set's files with the main file replaced, so extra files stay in the set.</summary>
        private static void Save(SyncedConfiguration synced, YamlFileSet set, string path, string text)
        {
            Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [path] = text };
            foreach (KeyValuePair<string, string> file in set.Files)
                if (!files.ContainsKey(file.Key))
                    files[file.Key] = file.Value;
            synced.Yaml.Replace(set, files, saveToDisk: true);
        }

        internal static string MainFilePath(SyncedConfiguration synced, YamlFileSet set)
        {
            string known = set.Files.Keys.FirstOrDefault(k => string.Equals(Path.GetFileName(k), set.MainFileName, StringComparison.OrdinalIgnoreCase));
            return known ?? Path.Combine(Path.GetDirectoryName(synced.Config.ConfigFilePath) ?? "", set.MainFileName);
        }

        internal static string DefaultText(YamlFileSet set)
        {
            byte[] bytes = set.DefaultContent?.Invoke();
            return bytes != null ? new UTF8Encoding(false).GetString(bytes) : null;
        }

        /// <summary>
        /// True while the lines after <paramref name="key"/> equal the default's: the player has not touched the entries,
        /// whatever header an older default wrote above them.
        /// </summary>
        internal static bool IsDefault(string text, string template, string key) => Body(text, key) == Body(template, key);

        private static string Body(string text, string key)
        {
            string[] lines = Normalize(text).Split('\n');
            int keyLine = Array.FindIndex(lines, line => line.TrimEnd() == key);
            return string.Join("\n", lines.Skip(keyLine + 1));
        }

        /// <summary>The 1.1.0 default commented its examples "#  entry"; both spellings count as still default.</summary>
        private static string Normalize(string text) => text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace("\n#  ", "\n  # ").TrimEnd();

        /// <summary>The template up to and including the containers: line, then one commented line per prefab.</summary>
        public static string Generate(string template, IReadOnlyDictionary<string, Container> prefabs)
        {
            StringBuilder text = new StringBuilder();
            foreach (string line in Normalize(template).Split('\n'))
            {
                text.Append(line).Append('\n');
                if (line.TrimEnd() == ContainersKey)
                    break;
            }
            if (!text.ToString().Contains(ContainersKey + "\n"))
                text.Append(ContainersKey).Append('\n');
            foreach (KeyValuePair<string, Container> prefab in prefabs.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                text.Append(CommentedLine(prefab.Key, prefab.Value)).Append('\n');
            return text.ToString();
        }

        /// <summary>
        /// The file with a commented line for every prefab no file of the set names, or null when none is missing.
        /// A name with a colon cannot be a plain key and is never listed, so nothing is appended twice.
        /// </summary>
        private static string AppendMissing(string text, string path, YamlFileSet set, IReadOnlyDictionary<string, Container> prefabs)
        {
            HashSet<string> named = NamedKeys(text, path, set);
            List<KeyValuePair<string, Container>> missing = prefabs.Where(p => !named.Contains(p.Key) && p.Key.IndexOf(':') < 0)
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToList();
            if (missing.Count == 0)
                return null;
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            StringBuilder result = new StringBuilder(text);
            if (text.Length > 0 && !text.EndsWith("\n"))
                result.Append(newline);
            if (!named.Contains("containers"))
                result.Append(ContainersKey).Append(newline);
            foreach (KeyValuePair<string, Container> prefab in missing)
                result.Append(CommentedLine(prefab.Key, prefab.Value)).Append(newline);
            return result.ToString();
        }

        /// <summary>Every key the main file (as on disk) and the set's other files name, commented out or not.</summary>
        private static HashSet<string> NamedKeys(string text, string path, YamlFileSet set)
        {
            HashSet<string> named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddKeys(named, text);
            foreach (KeyValuePair<string, string> file in set.Files)
                if (!string.Equals(file.Key, path, StringComparison.OrdinalIgnoreCase))
                    AddKeys(named, file.Value);
            return named;
        }

        private static void AddKeys(HashSet<string> named, string text)
        {
            foreach (string line in text.Split('\n'))
            {
                Match match = KeyLine.Match(line);
                if (match.Success)
                    named.Add(match.Groups[2].Value.Trim());
            }
        }

        private static string CommentedLine(string name, Container prefab)
        {
            ContainerSize vanilla = VanillaSizes.Remember(name, prefab);
            return "  # " + name + ": { width: " + vanilla.Width + ", height: " + vanilla.Height + " }";
        }
    }
}
