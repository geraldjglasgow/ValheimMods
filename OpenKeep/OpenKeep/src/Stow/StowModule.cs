using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Stow
{
    /// <summary>Entry point of the Stow module: binds its settings, registers its YAML files and language words.</summary>
    public static class StowModule
    {
        public const string Resource = "OpenKeep.config.OpenKeep.Stow.yml";

        public static YamlFileSet Files { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            StowSettings.Bind(synced);
            Files = synced.AddYaml(new YamlFileSet("OpenKeep.Stow*.yml", "openkeep_stow", () => new StowModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(StowModule).Assembly, Resource),
                EditorLabel = () => Language.Localize(StowWords.Edit),
            });
            StowWords.Register();
            StowSettings.ButtonRowOffset.SettingChanged += Guard.Wrap("stow button row offset", (_, _) => PanelButtons.Reposition());
        }

        private static void Apply(YamlModel model)
        {
            StowModel rules = (StowModel)model;
            StowRules.Apply(rules);
            Plugin.Log.LogInfo($"Stow rules applied: {rules.Containers.Count} container rules.");
        }
    }
}
