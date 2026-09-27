using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using EarthWright.Core;
using SyncedConfig;
using YamlConfig;

namespace EarthWright.Menu
{
    /// <summary>
    /// The custom menu entries of EarthWright.Entries*.yml. The file set is synced: the server's file reaches every player
    /// and edits are hot reloaded, and every machine then registers the same actions and words and rebuilds the same
    /// entry prefabs (<see cref="CustomPrefabs"/>, on the next frame through <see cref="MenuRefresh"/>).
    /// </summary>
    public static class CustomEntries
    {
        public const string Resource = "EarthWright.config.EarthWright.Entries.yml";

        private static List<CustomEntry> entries = new List<CustomEntry>();
        private static string signature = "";

        /// <summary>Counts the changes of the entry list, so per-frame readers notice a reload.</summary>
        public static int Version { get; private set; }

        public static IReadOnlyList<CustomEntry> All => entries;

        public static void Register(SyncedConfiguration synced)
        {
            synced.AddYaml(new YamlFileSet("EarthWright.Entries*.yml", "earthwright_entries", () => new CustomEntriesModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(CustomEntries).Assembly, Resource),
                EditorLabel = () => Language.Localize(MenuWords.EditEntries),
            });
        }

        public static CustomEntry ById(string id) => id == null ? null : entries.FirstOrDefault(e => e.Id == id);

        public static CustomEntry ByPrefab(string prefabName) => prefabName == null ? null : entries.FirstOrDefault(e => e.PrefabName == prefabName);

        /// <summary>The entry a special action belongs to ("custom:&lt;id&gt;"), or null.</summary>
        public static CustomEntry ForAction(ToolAction action)
        {
            if (action == null || action.Special == null || !action.Special.StartsWith("custom:"))
                return null;
            return ById(action.Special.Substring("custom:".Length));
        }

        private static void Apply(YamlModel model)
        {
            List<CustomEntry> loaded = ((CustomEntriesModel)model).Entries;
            string loadedSignature = string.Join("\n", loaded.Select(e => e.Signature()));
            if (loadedSignature == signature)
                return;
            signature = loadedSignature;
            entries = loaded;
            Version++;
            foreach (CustomEntry entry in entries)
                ActionCatalog.Register(entry.CreateAction());
            MenuRefresh.CustomsChanged();
            Plugin.Log.LogInfo($"EarthWright: {entries.Count} custom menu entries loaded.");
        }
    }
}
