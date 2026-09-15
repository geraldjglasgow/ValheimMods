using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Salvage
{
    /// <summary>Entry point of the Salvage module: binds its settings, registers its YAML files and language words.</summary>
    public static class SalvageModule
    {
        public const string Resource = "OpenKeep.config.OpenKeep.Salvage.yml";

        public static YamlFileSet Files { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            SalvageSettings.Bind(synced);
            Files = synced.AddYaml(new YamlFileSet("OpenKeep.Salvage*.yml", "openkeep_salvage", () => new SalvageModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(SalvageModule).Assembly, Resource),
                EditorLabel = () => Core.Language.Localize(SalvageWords.Edit),
            });
            SalvageWords.Register();
        }

        private static void Apply(YamlModel model)
        {
            SalvageModel rules = (SalvageModel)model;
            SalvageRules.Apply(rules);
            Plugin.Log.LogInfo($"Salvage rules applied: {rules.Overrides.Count} overrides, deny list {(rules.Deny.IsEmpty ? "none" : "set")}.");
        }
    }
}
