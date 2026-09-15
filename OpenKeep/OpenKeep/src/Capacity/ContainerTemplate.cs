using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// Fills the freshly written OpenKeep.Containers.yml with every container prefab of the scene and its vanilla
    /// size, all commented out, the first time a world loads. Only on the author, and only while the file
    /// on disk still equals the embedded default, so a file the player has touched is never rewritten.
    /// </summary>
    public static class ContainerTemplate
    {
        private const string ContainersKey = "containers:";

        public static void GenerateIfDefault()
        {
            SyncedConfiguration synced = CapacityModule.Synced;
            YamlFileSet set = CapacityModule.Set;
            if (synced == null || set == null || !synced.Yaml.IsAuthor || ZNetScene.instance == null)
                return;
            string path = MainFilePath(synced, set);
            if (!File.Exists(path))
                return;
            string template = DefaultText(set);
            if (template == null || Normalize(File.ReadAllText(path)) != Normalize(template))
                return;
            string generated = Generate(template, ContainerPrefabs.All());
            synced.Yaml.Replace(set, new Dictionary<string, string> { [path] = generated }, saveToDisk: true);
            Plugin.Log.LogInfo($"OpenKeep: listed every container prefab in {path}.");
        }

        private static string MainFilePath(SyncedConfiguration synced, YamlFileSet set)
        {
            string known = set.Files.Keys.FirstOrDefault(k => string.Equals(Path.GetFileName(k), set.MainFileName, StringComparison.OrdinalIgnoreCase));
            return known ?? Path.Combine(Path.GetDirectoryName(synced.Config.ConfigFilePath) ?? "", set.MainFileName);
        }

        private static string DefaultText(YamlFileSet set)
        {
            byte[] bytes = set.DefaultContent?.Invoke();
            return bytes != null ? new UTF8Encoding(false).GetString(bytes) : null;
        }

        private static string Normalize(string text) => text.TrimStart('\uFEFF').Replace("\r\n", "\n").TrimEnd();

        /// <summary>The template up to and including the containers: line, then one commented line per prefab.</summary>
        public static string Generate(string template, Dictionary<string, Container> prefabs)
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
            {
                ContainerSize vanilla = VanillaSizes.Remember(prefab.Key, prefab.Value);
                text.Append("#  ").Append(prefab.Key).Append(": { width: ").Append(vanilla.Width).Append(", height: ").Append(vanilla.Height).Append(" }\n");
            }
            return text.ToString();
        }
    }
}
