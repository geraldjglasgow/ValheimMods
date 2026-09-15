using System;
using HarmonyLib;
using ItemCopies;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Stacks
{
    /// <summary>Entry point of the Stacks module: binds its settings, registers its YAML files and language words.</summary>
    public static class StacksModule
    {
        public const string DefaultResource = "OpenKeep.config.OpenKeep.Stacks.yml";

        public static SyncedConfiguration Synced { get; private set; }

        /// <summary>The OpenKeep.Stacks*.yml set; its Current model is the last one that parsed without errors.</summary>
        public static YamlFileSet Set { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            Synced = synced;
            StacksSettings.Bind(synced);
            Set = synced.AddYaml(new YamlFileSet("OpenKeep.Stacks*.yml", "openkeep_stacks", () => new StacksModel(), ApplyYaml)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(StacksModule).Assembly, DefaultResource),
                EditorLabel = () => "Edit stacks and weights",
            });
            HookSettings(synced);
        }

        /// <summary>New world items and items entering an inventory carry their own shared data copy; it gets the current values.</summary>
        public static void HookSpawns(Harmony harmony) => Copies.HookSpawns(harmony, StackValues.ApplyCopy);

        /// <summary>Every change of a section 4 entry, and a reload of the .cfg, re-applies the values.</summary>
        private static void HookSettings(SyncedConfiguration synced)
        {
            EventHandler apply = Guard.Wrap("stacks settings", (_, _) => StackValues.ApplyAll());
            StacksSettings.Enabled.SettingChanged += apply;
            StacksSettings.StackMultiplier.SettingChanged += apply;
            StacksSettings.WeightMultiplier.SettingChanged += apply;
            StacksSettings.PerItemConfigEntries.SettingChanged += apply;
            synced.Config.ConfigReloaded += Guard.Wrap("stacks reload", (_, _) => StackValues.ApplyAll());
        }

        private static void ApplyYaml(YamlModel model)
        {
            StackValues.ApplyAll();
            Documentation.WriteIfReady();
            Plugin.Log.LogInfo($"OpenKeep: stack rules applied, {((StacksModel)model).Rules.Count} item entries.");
        }
    }
}
