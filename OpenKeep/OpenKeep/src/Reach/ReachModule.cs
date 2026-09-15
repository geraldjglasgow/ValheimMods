using OpenKeep.Core;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Reach
{
    /// <summary>Entry point of the Reach module: binds its settings, registers its YAML files and language words.</summary>
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
                EditorLabel = () => "Edit reach rules",
            });
            Words();
        }

        private static void Words()
        {
            Language.Add("ok_fromstorage", "From storage");
            Language.Add("ok_reach", "OpenKeep reach");
            Language.Add("ok_on", "on");
            Language.Add("ok_off", "off");
            Language.Add("ok_pulled", "Pulled from storage:");
            Language.Add("ok_nothingtopull", "Nothing to pull from storage");
            Language.Add("ok_nofit", "The materials from storage do not fit in the inventory");
        }
    }
}
