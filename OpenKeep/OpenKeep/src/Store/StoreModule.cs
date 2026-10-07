using System.IO;
using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Store
{
    /// <summary>Entry point of the Store module: binds its settings, registers its YAML files and language words.</summary>
    public static class StoreModule
    {
        public const string Resource = "OpenKeep.config.OpenKeep.Store.yml";

        public static YamlFileSet Files { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            StoreSettings.Bind(synced);
            RenameOldFiles(synced);
            Files = synced.AddYaml(new YamlFileSet("OpenKeep.Store*.yml", "openkeep_store", () => new StoreModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(StoreModule).Assembly, Resource),
                EditorLabel = () => Language.Localize(StoreWords.Edit),
            });
            StoreWords.Register();
            StoreSettings.ButtonRowOffset.SettingChanged += Guard.Wrap("store button row offset", (_, _) => PanelButtons.Reposition());
        }

        /// <summary>Renames the rule files of 3.0.0 and earlier (OpenKeep.Stow*.yml) to OpenKeep.Store*.yml, before
        /// they are looked for, unless a file of the new name is already there.</summary>
        private static void RenameOldFiles(SyncedConfiguration synced)
        {
            foreach (string folder in synced.SearchPaths)
            {
                if (!Directory.Exists(folder))
                    continue;
                foreach (string old in Directory.GetFiles(folder, "OpenKeep.Stow*.yml"))
                    RenameOldFile(old);
            }
        }

        private static void RenameOldFile(string old)
        {
            string renamed = Path.Combine(Path.GetDirectoryName(old), Path.GetFileName(old).Replace("OpenKeep.Stow", "OpenKeep.Store"));
            if (File.Exists(renamed))
                return;
            try
            {
                File.Move(old, renamed);
                Plugin.Log.LogInfo($"Renamed {Path.GetFileName(old)} to {Path.GetFileName(renamed)}");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Could not rename {old} to {Path.GetFileName(renamed)}: {e.Message}");
            }
        }

        private static void Apply(YamlModel model)
        {
            StoreModel rules = (StoreModel)model;
            StoreRules.Apply(rules);
            Plugin.Log.LogInfo($"Store rules applied: {rules.Containers.Count} container rules.");
        }
    }
}
