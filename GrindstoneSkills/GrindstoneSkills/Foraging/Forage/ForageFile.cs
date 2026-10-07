using SyncedConfig;
using YamlConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// GrindstoneSkills.Forage*.yml: registered with the synced configuration (written from the embedded default when
    /// missing, sent from the server to every player while the server binds the configuration, reloaded a few seconds
    /// after an edit, editable in game) and holding the entries of the last file set that parsed without errors. The
    /// picker's client reads them to decide what is forage and the experience of a pick.
    /// </summary>
    public static class ForageFile
    {
        public const string Pattern = "GrindstoneSkills.Forage*.yml";
        public const string SyncKey = Keys.ForageSync;
        public const string Resource = "GrindstoneSkills.config.GrindstoneSkills.Forage.yml";

        private static ForageModel current = new ForageModel();

        public static void Register(SyncedConfiguration config)
        {
            config.AddYaml(new YamlFileSet(Pattern, SyncKey, () => new ForageModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(ForageFile).Assembly, Resource),
            });
        }

        /// <summary>The entry of an item prefab, or null when the file does not list it.</summary>
        public static ForageEntry Find(string itemPrefab) =>
            !string.IsNullOrEmpty(itemPrefab) && current.Items.TryGetValue(itemPrefab, out ForageEntry entry) ? entry : null;

        private static void Apply(YamlModel model)
        {
            current = (ForageModel)model;
            GrindstoneSkills.Log.LogInfo($"Forage applied: {current.Items.Count} items.");
        }
    }
}
