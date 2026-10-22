using SyncedConfig;
using Translations;
using YamlConfig;

namespace OpenKeep.Reach
{
    /// <summary>Entry point of the Reach module: binds its settings and registers its YAML files.</summary>
    public static class ReachModule
    {
        public const string Resource = "OpenKeep.config.OpenKeep.Reach.yml";

        public static YamlFileSet Files { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            ReachSettings.Initialize(synced);
            Files = synced.AddYaml(new YamlFileSet("OpenKeep.Reach*.yml", "openkeep_reach", () => new ReachModel(), ReachRules.Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(ReachModule).Assembly, Resource),
                EditorLabel = () => Lang.Get("ok_reach_edit"),
            });
        }
    }
}
