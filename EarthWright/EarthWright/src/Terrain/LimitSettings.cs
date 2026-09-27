using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using YamlConfig;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Section "6. Height Limits": how far the ground may be raised above and dug below the world's generated height,
    /// the admin limit, and the per-biome overrides of <c>EarthWright.Limits*.yml</c> (synced from the server, hot
    /// reloaded). Everything here is synced: every client must draw the same ground and every owner enforce the same limits.
    /// </summary>
    public static class LimitSettings
    {
        public const string DefaultResource = "EarthWright.config.EarthWright.Limits.yml";

        public static ConfigEntry<float> RaiseLimit { get; private set; }
        public static ConfigEntry<float> DigLimit { get; private set; }
        public static ConfigEntry<float> AdminLimit { get; private set; }

        /// <summary>The EarthWright.Limits*.yml set; its Current model is the last one that parsed without errors.</summary>
        public static YamlFileSet Files { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            RaiseLimit = synced.Bind(Sections.Limits, "Raise Limit", 8f,
                "How far the ground may be raised above the world's generated height, in metres (the unmodded game allows 8). Repeated raises add up to this. Applies to new edits; ground already higher stays as it is.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 512f));
            DigLimit = synced.Bind(Sections.Limits, "Dig Limit", 8f,
                "How deep the ground may be dug below the world's generated height, in metres (the unmodded game allows 8). Also where the pickaxe stops dropping stone. Applies to new edits.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 512f));
            AdminLimit = synced.Bind(Sections.Limits, "Admin Limit", 256f,
                "The most an admin's edit that ignores the limits (admin tools, the limit override key) may raise or dig, in metres.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 512f));
            Files = synced.AddYaml(new YamlFileSet("EarthWright.Limits*.yml", "earthwright_limits", () => new LimitsModel(), ApplyYaml)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(LimitSettings).Assembly, DefaultResource),
                EditorLabel = () => "Edit height limits per biome",
            });
        }

        private static void ApplyYaml(YamlModel model)
        {
            LimitsModel limits = (LimitsModel)model;
            HeightLimits.SetBiomeRules(limits.Rules);
            Plugin.Log.LogInfo($"EarthWright: height limits per biome applied, {limits.Count} biomes.");
        }

        public static float RaiseValue => RaiseLimit != null ? RaiseLimit.Value : 8f;

        public static float DigValue => DigLimit != null ? DigLimit.Value : 8f;

        public static float AdminValue => AdminLimit != null ? AdminLimit.Value : 256f;
    }
}
