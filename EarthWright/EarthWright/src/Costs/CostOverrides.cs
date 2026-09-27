using System;
using System.Collections.Generic;
using EarthWright.Core;
using SyncedConfig;
using YamlConfig;

namespace EarthWright.Costs
{
    /// <summary>
    /// Registers EarthWright.Costs*.yml (synced from the server, hot reloaded, editable in game) and holds the entry
    /// overrides of the last file set that parsed without errors.
    /// </summary>
    public static class CostOverrides
    {
        public const string Resource = "EarthWright.config.EarthWright.Costs.yml";

        private static Dictionary<string, EntryOverride> entries = new Dictionary<string, EntryOverride>(StringComparer.OrdinalIgnoreCase);

        public static YamlFileSet Files { get; private set; }

        public static void Register(SyncedConfiguration synced)
        {
            Files = synced.AddYaml(new YamlFileSet("EarthWright.Costs*.yml", "earthwright_costs", () => new CostsModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(CostOverrides).Assembly, Resource),
                EditorLabel = () => Language.Localize(CostWords.Edit),
            });
        }

        /// <summary>The overrides of a menu entry by its prefab name, or null when the file sets none.</summary>
        public static EntryOverride For(string entryId)
        {
            return entryId != null && entries.TryGetValue(entryId, out EntryOverride entry) ? entry : null;
        }

        private static void Apply(YamlModel model)
        {
            entries = ((CostsModel)model).Entries;
            ItemLookup.ResetWarnings();
            Plugin.Log.LogInfo($"Cost overrides applied: {entries.Count} entries.");
        }
    }
}
