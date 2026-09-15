using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Signs
{
    /// <summary>Entry point of the Signs module: binds its settings, registers its YAML files and language words.</summary>
    public static class SignsModule
    {
        public const string DefaultResource = "OpenKeep.config.OpenKeep.Signs.yml";

        /// <summary>The OpenKeep.Signs*.yml set; its Current model is the last one that parsed without errors.</summary>
        public static YamlFileSet Set { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            SignsSettings.Bind(synced);
            Set = synced.AddYaml(new YamlFileSet("OpenKeep.Signs*.yml", "openkeep_signs", () => new SignsModel(), ApplyYaml)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(SignsModule).Assembly, DefaultResource),
                EditorLabel = () => "Edit sign rules",
            });
            SignsSettings.Height.SettingChanged += Guard.Wrap("signs layout", (_, _) => SignRefresh.RulesChanged());
            SignsSettings.Rotation.SettingChanged += Guard.Wrap("signs layout", (_, _) => SignRefresh.RulesChanged());
            SignsSettings.ShowCounts.SettingChanged += Guard.Wrap("signs text", (_, _) => SignRefresh.RulesChanged());
            SignsSettings.MaxItems.SettingChanged += Guard.Wrap("signs text", (_, _) => SignRefresh.RulesChanged());
            SignsSettings.MaxCharacters.SettingChanged += Guard.Wrap("signs text", (_, _) => SignRefresh.RulesChanged());
            SignsSettings.EmptyText.SettingChanged += Guard.Wrap("signs text", (_, _) => SignRefresh.RulesChanged());
            Words();
        }

        private static void Words()
        {
            Language.Add("ok_signs_sign", "sign");
            Language.Add("ok_signs_playertext", "player text");
            Language.Add("ok_signs_nosign", "no sign");
            Language.Add("ok_signs_optedout", "opted out");
            Language.Add("ok_signs_reset", "Signs allowed again on {0} containers.");
            Language.Add("ok_signs_rewritten", "{0} signs rewritten.");
        }

        private static void ApplyYaml(YamlModel model)
        {
            SignRefresh.RulesChanged();
            Plugin.Log.LogInfo($"OpenKeep: sign rules applied, {((SignsModel)model).Containers.Count} prefabs.");
        }
    }
}
