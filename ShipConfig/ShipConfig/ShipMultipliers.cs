using BepInEx.Configuration;
using PatchGuard;
using SyncedConfig;

namespace ShipConfig
{
    /// <summary>
    /// Section Multipliers: global factors on top of the per-ship values, all default 1, synced and lockable.
    /// Values below 0 count as 0. A change re-applies every ship.
    /// </summary>
    public static class ShipMultipliers
    {
        public const string Section = "Multipliers";

        public static ConfigEntry<float> HealthMultiplier { get; private set; }
        public static ConfigEntry<float> SailForceMultiplier { get; private set; }
        public static ConfigEntry<float> PaddleForceMultiplier { get; private set; }
        public static ConfigEntry<float> TurningMultiplier { get; private set; }
        public static ConfigEntry<float> DamageTakenMultiplier { get; private set; }
        public static ConfigEntry<float> BuildCostMultiplier { get; private set; }

        public static float Health => Positive(HealthMultiplier);
        public static float SailForce => Positive(SailForceMultiplier);
        public static float PaddleForce => Positive(PaddleForceMultiplier);
        public static float Turning => Positive(TurningMultiplier);
        public static float DamageTaken => Positive(DamageTakenMultiplier);
        public static float BuildCost => Positive(BuildCostMultiplier);

        public static void Initialize(SyncedConfiguration config)
        {
            HealthMultiplier = Bind(config, "Health Multiplier", "health");
            SailForceMultiplier = Bind(config, "Sail Force Multiplier", "sail force");
            PaddleForceMultiplier = Bind(config, "Paddle Force Multiplier", "paddle force");
            TurningMultiplier = Bind(config, "Turning Multiplier", "turning force");
            DamageTakenMultiplier = Bind(config, "Damage Taken Multiplier", "damage taken");
            BuildCostMultiplier = Bind(config, "Build Cost Multiplier", "build cost");
        }

        private static ConfigEntry<float> Bind(SyncedConfiguration config, string key, string subject)
        {
            ConfigEntry<float> entry = config.Bind(Section, key, 1f,
                $"Multiplier on the {subject} of every ship, on top of the per-ship value. 1 is vanilla.");
            entry.SettingChanged += Guard.Wrap($"apply {key}", (_, _) => ShipValues.ApplyAll());
            return entry;
        }

        /// <summary>The entry's value, 0 for negative values, 1 before the entry is bound.</summary>
        private static float Positive(ConfigEntry<float> entry)
        {
            if (entry == null)
                return 1f;
            return entry.Value < 0f ? 0f : entry.Value;
        }
    }
}
