using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Mountains
{
    public sealed class MountainSettings
    {
        private static readonly Dictionary<MountainKind, MountainSettings> Values = new Dictionary<MountainKind, MountainSettings>();
        private ConfigEntry<bool> enabled = null!;
        private ConfigEntry<float> health = null!, damage = null!, chance = null!, interval = null!;
        public bool Enabled => enabled.Value;
        public float Health => health.Value;
        public float Damage => damage.Value;
        public float Chance => chance.Value;
        public float Interval => interval.Value;
        public static MountainSettings For(MountainKind kind) => Values[kind];
        public static void Initialize(SyncedConfiguration config)
        {
            for (int i = 0; i < MountainKind.All.Length; ++i)
            {
                MountainKind kind = MountainKind.All[i];
                var value = new MountainSettings();
                value.Bind(config, (15 + i) + " - " + kind.Name, kind);
                Values[kind] = value;
            }
        }
        private void Bind(SyncedConfiguration config, string section, MountainKind kind)
        {
            enabled = config.Bind(section, "Enabled", true, "Allow wild Mountain encounters. Existing creatures stay when disabled.");
            health = config.Bind(section, "Health", kind.Health, "Base health before stars. Applies to new creatures.", acceptableValues: Settings.Range(1f, 10000f));
            damage = config.Bind(section, "Damage Factor", kind.Damage, "Multiplier on the creature's configured attacks; reload applies to loaded creatures.", acceptableValues: Settings.Range(0f, 20f));
            chance = config.Bind(section, "Spawn Chance", kind.Chance, "Percent chance per attempt in occupied Mountain zones.", acceptableValues: Settings.Range(0f, 100f));
            interval = config.Bind(section, "Spawn Interval", kind.Interval, "Seconds between wild spawn attempts per Mountain zone.", acceptableValues: Settings.Range(30f, 7200f));
        }
    }
}
