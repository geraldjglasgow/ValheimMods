using PatchGuard;
using SyncedConfig;
using Translations;
using YamlConfig;

namespace OpenKeep.Capacity
{
    /// <summary>Entry point of the Capacity module: binds its settings and registers its YAML files.</summary>
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
                EditorLabel = () => Lang.Get("ok_containers_edit"),
            });
            CapacitySettings.Enabled.SettingChanged += Guard.Wrap("capacity switch", (_, _) => ContainerSizes.ApplyAll());
            StationsFile.Initialize(synced);
        }

        private static void ApplyYaml(YamlModel model)
        {
            ContainerSizes.ApplyAll();
            Plugin.Log.LogInfo($"OpenKeep: container sizes applied, {((ContainersModel)model).Sizes.Count} prefabs.");
        }
    }
}
