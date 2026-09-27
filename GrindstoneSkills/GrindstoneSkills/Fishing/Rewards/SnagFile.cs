using System.Collections.Generic;
using SyncedConfig;
using YamlConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills.Snags*.yml: registered with the synced configuration on every machine (written from the embedded
    /// default when missing, sent from the server to every player while the server binds the configuration, reloaded a
    /// few seconds after an edit, editable in game) and holding the tables of the last file set that parsed without
    /// errors. The tables hold only names, so applying them before the item database exists is safe. The same pattern as
    /// the Mine Finds (<see cref="MineFindFile"/>), per biome only.
    /// </summary>
    public static class SnagFile
    {
        public const string Pattern = "GrindstoneSkills.Snags*.yml";
        public const string SyncKey = Keys.SnagsSync;
        public const string Resource = "GrindstoneSkills.config.GrindstoneSkills.Snags.yml";

        private static SnagModel current = new SnagModel();

        public static void Register(SyncedConfiguration config)
        {
            config.AddYaml(new YamlFileSet(Pattern, SyncKey, () => new SnagModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(SnagFile).Assembly, Resource),
            });
        }

        /// <summary>One snag for a cast in this biome, picked by weight; null when the biome has none.</summary>
        public static FindEntry Pick(Heightmap.Biome biome) =>
            current.ByBiome.TryGetValue(biome, out List<FindEntry> table) ? FindFile.PickFrom(table) : null;

        private static void Apply(YamlModel model)
        {
            current = (SnagModel)model;
            FindPrefabs.ResetWarnings();
            GrindstoneSkills.Log.LogInfo($"Snags applied: {current.ByBiome.Count} biomes, {current.Count} snags.");
        }
    }
}
