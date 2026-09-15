using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;
using YamlConfig;

namespace OpenKeep.Capacity
{
    /// <summary>Entry point of the Capacity module: binds its settings, registers its YAML files and language words.</summary>
    public static class CapacityModule
    {
        public const string DefaultResource = "OpenKeep.config.OpenKeep.Containers.yml";

        public static SyncedConfiguration Synced { get; private set; }

        /// <summary>The OpenKeep.Containers*.yml set; its Current model is the last one that parsed without errors.</summary>
        public static YamlFileSet Set { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            Synced = synced;
            CapacitySettings.Bind(synced);
            Set = synced.AddYaml(new YamlFileSet("OpenKeep.Containers*.yml", "openkeep_containers", () => new ContainersModel(), ApplyYaml)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(CapacityModule).Assembly, DefaultResource),
                EditorLabel = () => "Edit container sizes",
            });
            CapacitySettings.Enabled.SettingChanged += Guard.Wrap("capacity switch", (_, _) => ContainerSizes.ApplyAll());
            Language.Add("ok_slots", "slots");
            Language.Add("ok_full", "full");
            Language.Add("ok_and", "and");
            Language.Add("ok_more", "more");
        }

        private static void ApplyYaml(YamlModel model)
        {
            ContainerSizes.ApplyAll();
            Plugin.Log.LogInfo($"OpenKeep: container sizes applied, {((ContainersModel)model).Sizes.Count} prefabs.");
        }
    }
}
