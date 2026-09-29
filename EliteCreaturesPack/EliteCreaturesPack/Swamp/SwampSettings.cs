using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Server-synced encounter and combat settings, one section per creature.</summary>
    public sealed class SwampSettings
    {
        private static readonly Dictionary<SwampKind, SwampSettings> Values = new Dictionary<SwampKind, SwampSettings>();
        private ConfigEntry<bool> enabled = null!;
        private ConfigEntry<float> health = null!, damage = null!, chance = null!, interval = null!;
        public bool Enabled => enabled.Value;
        public float Health => health.Value;
        public float Damage => damage.Value;
        public float Chance => chance.Value;
        public float Interval => interval.Value;
        public static SwampSettings For(SwampKind kind) => Values[kind];

        public static void Initialize(SyncedConfiguration config)
        {
            for (int i = 0; i < SwampKind.All.Length; ++i)
            {
                SwampKind kind = SwampKind.All[i];
                var settings = new SwampSettings();
                settings.Bind(config, (10 + i) + " - " + kind.Name, kind);
                Values[kind] = settings;
            }
        }

        private void Bind(SyncedConfiguration config, string section, SwampKind kind)
        {
            enabled = config.Bind(section, "Enabled", true, "Allow wild swamp encounters. Existing creatures stay when disabled.");
            health = config.Bind(section, "Health", kind.Health, "Base health before stars. Applies to new creatures.",
                acceptableValues: Settings.Range(1f, 10000f));
            damage = config.Bind(section, "Damage Factor", kind.Damage, "Attack damage multiplier; follows reload on loaded creatures.",
                acceptableValues: Settings.Range(0f, 20f));
            chance = config.Bind(section, "Spawn Chance", kind.Chance, "Percent chance per spawn interval in occupied swamp zones.",
                acceptableValues: Settings.Range(0f, 100f));
            interval = config.Bind(section, "Spawn Interval", kind.Interval, "Seconds between wild spawn attempts per swamp zone.",
                acceptableValues: Settings.Range(30f, 7200f));
        }
    }
}
