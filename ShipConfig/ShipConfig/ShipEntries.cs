using BepInEx.Configuration;
using PatchGuard;
using SyncedConfig;

namespace ShipConfig
{
    /// <summary>
    /// The config entries of one ship prefab, all in section Ship and keyed Prefab.Key, and their effective values:
    /// the per-ship value times the global multiplier, never below 0. Every entry is synced and lockable; a change
    /// re-applies that ship.
    /// </summary>
    public sealed class ShipEntries
    {
        public const string Section = "Ship";

        public string Name { get; }

        public ConfigEntry<float> Health { get; private set; }
        public ConfigEntry<float> SailForce { get; private set; }
        public ConfigEntry<float> PaddleForce { get; private set; }
        public ConfigEntry<float> RudderSpeed { get; private set; }
        public ConfigEntry<float> TurnForceSailing { get; private set; }
        public ConfigEntry<float> TurnForcePaddling { get; private set; }
        public ConfigEntry<float> ForwardDrag { get; private set; }
        public ConfigEntry<float> SidewaysDrag { get; private set; }
        public ConfigEntry<float> AngularDamping { get; private set; }
        public ConfigEntry<float> WaterImpactDamage { get; private set; }
        public ConfigEntry<float> UpsideDownDamage { get; private set; }
        public ConfigEntry<bool> WeatherWear { get; private set; }
        public ConfigEntry<bool> AshlandsOceanDamage { get; private set; }
        public ConfigEntry<float> DamageTaken { get; private set; }
        public ConfigEntry<bool> Invulnerable { get; private set; }

        /// <summary>Null when the ship has no Piece component (nothing to build).</summary>
        public ConfigEntry<float> BuildCost { get; private set; }

        /// <summary>The vanilla material amounts, recorded once so repeated changes never compound; null without a Piece.</summary>
        public int[] OriginalBuildCosts { get; }

        private ShipEntries(string name, int[] originalBuildCosts)
        {
            Name = name;
            OriginalBuildCosts = originalBuildCosts;
        }

        public float EffectiveHealth => Positive(Health.Value) * ShipMultipliers.Health;
        public float EffectiveSailForce => Positive(SailForce.Value) * ShipMultipliers.SailForce;
        public float EffectivePaddleForce => Positive(PaddleForce.Value) * ShipMultipliers.PaddleForce;
        public float EffectiveRudderSpeed => Positive(RudderSpeed.Value);
        public float EffectiveTurnForceSailing => Positive(TurnForceSailing.Value) * ShipMultipliers.Turning;
        public float EffectiveTurnForcePaddling => Positive(TurnForcePaddling.Value) * ShipMultipliers.Turning;
        public float EffectiveForwardDrag => Positive(ForwardDrag.Value);
        public float EffectiveSidewaysDrag => Positive(SidewaysDrag.Value);
        public float EffectiveAngularDamping => Positive(AngularDamping.Value);
        public float EffectiveWaterImpactDamage => Positive(WaterImpactDamage.Value);
        public float EffectiveUpsideDownDamage => Positive(UpsideDownDamage.Value);
        public float EffectiveDamageTaken => Positive(DamageTaken.Value) * ShipMultipliers.DamageTaken;
        public float EffectiveBuildCost => BuildCost == null ? 1f : Positive(BuildCost.Value) * ShipMultipliers.BuildCost;

        /// <summary>Invulnerable, or an effective damage multiplier of 0: the ship takes no damage at all.</summary>
        public bool TakesNoDamage => Invulnerable.Value || EffectiveDamageTaken <= 0f;

        /// <summary>Binds every entry of one ship with its vanilla defaults.</summary>
        public static ShipEntries Bind(SyncedConfiguration synced, string name, ShipDefaults defaults)
        {
            ShipEntries entries = new ShipEntries(name, defaults.BuildCosts);
            entries.Health = entries.BindEntry(synced, "Health", defaults.Health, $"Health of {name}.");
            entries.BindSailing(synced, defaults);
            entries.BindWater(synced, defaults);
            entries.BindDamage(synced, defaults);
            return entries;
        }

        private void BindSailing(SyncedConfiguration synced, ShipDefaults defaults)
        {
            SailForce = BindEntry(synced, "SailForce", defaults.SailForce,
                "Thrust from the sail. Top speed under sail, scaled by wind and sail size.");
            PaddleForce = BindEntry(synced, "PaddleForce", defaults.PaddleForce,
                "Force when paddling forward (slow) and backward.");
            RudderSpeed = BindEntry(synced, "RudderSpeed", defaults.RudderSpeed,
                "How fast the rudder swings to full lock while steering.");
            TurnForceSailing = BindEntry(synced, "TurnForceSailing", defaults.TurnForceSailing,
                "Turning force while under sail, grows with forward speed.");
            TurnForcePaddling = BindEntry(synced, "TurnForcePaddling", defaults.TurnForcePaddling,
                "Turning force while paddling.");
        }

        private void BindWater(SyncedConfiguration synced, ShipDefaults defaults)
        {
            ForwardDrag = BindEntry(synced, "ForwardDrag", defaults.ForwardDrag,
                "Forward water drag. Lower is faster and coasts longer; this caps the top speed.");
            SidewaysDrag = BindEntry(synced, "SidewaysDrag", defaults.SidewaysDrag,
                "Sideways water drag. Lower lets the ship slide in a crosswind. Tune with care.");
            AngularDamping = BindEntry(synced, "AngularDamping", defaults.AngularDamping,
                "Resistance to rolling and pitching. Higher is a steadier deck; too high and the ship stops turning. Tune with care.");
            WaterImpactDamage = BindEntry(synced, "WaterImpactDamage", defaults.WaterImpactDamage,
                "Damage the ship takes from slamming into rough seas with players aboard. 0 turns it off.");
            UpsideDownDamage = BindEntry(synced, "UpsideDownDamage", defaults.UpsideDownDamage,
                "Damage per second while the ship is capsized. 0 turns it off.");
        }

        private void BindDamage(SyncedConfiguration synced, ShipDefaults defaults)
        {
            WeatherWear = BindEntry(synced, "WeatherWear", defaults.WeatherWear,
                "true: the ship loses health in rain and when its hull is under water, down to half health, like vanilla. false: no weather wear.");
            AshlandsOceanDamage = BindEntry(synced, "AshlandsOceanDamage", defaults.AshlandsOceanDamage,
                "true: the burning Ashlands ocean damages this ship, like vanilla for every ship except the Drakkar. false: this ship sails the Ashlands ocean unharmed. WARNING: turning this off removes the progression gate that makes the Drakkar necessary, and the Ashlands ocean effects and world key it triggers.");
            DamageTaken = BindEntry(synced, "DamageTaken", 1f,
                "Multiplier on every hit the ship takes: attacks, collisions, rough seas, capsizing, Ashlands ocean. 1 is vanilla, 0.5 halves it, 0 is the same as Invulnerable.");
            Invulnerable = BindEntry(synced, "Invulnerable", false,
                "true: the ship takes no damage at all, not from hits, weather or the Ashlands ocean. Ships cannot be dismantled, so an invulnerable ship can only be removed by turning this off.");
            if (defaults.BuildCosts != null)
                BuildCost = BindEntry(synced, "BuildCost", 1f,
                    "Multiplier on the materials needed to build this ship. 1 is vanilla. Each material is rounded to the nearest whole number and never drops below 1.");
        }

        /// <summary>Binds Prefab.Key in the Ship section, synced; a change re-applies this ship.</summary>
        private ConfigEntry<T> BindEntry<T>(SyncedConfiguration synced, string key, T defaultValue, string description)
        {
            ConfigEntry<T> entry = synced.Bind(Section, $"{Name}.{key}", defaultValue, description);
            entry.SettingChanged += Guard.Wrap($"apply {Name}.{key}", (_, _) => ShipValues.Apply(Name));
            return entry;
        }

        private static float Positive(float value) => value < 0f ? 0f : value;
    }
}
