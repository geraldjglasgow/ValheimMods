using BepInEx.Configuration;
using EliteCreaturesPack.Core;
using SyncedConfig;

namespace EliteCreaturesPack.RimeGiant
{
    /// <summary>
    /// Section 4: the Rime Giant. Whether it may appear, how rarely (the share of mountains that hold one, a chance each
    /// time a zone's spawner rolls, how often a zone rolls), where, and the numbers of its fight: its health, its rime
    /// armour (how many plates, how much of a physical hit gets through them, how much fire breaks one, at what health
    /// the rest shatter, how fast they grow back after a fight) and its attacks. Everything else is the game's forest
    /// troll it is built on. Synced; read where they apply, so a reload takes effect.
    /// </summary>
    public static class RimeGiantSettings
    {
        public const string Section = "4 - Rime Giant";

        private static ConfigEntry<bool> enabled = null!;
        private static ConfigEntry<float> mountains = null!, chance = null!, interval = null!;
        private static ConfigEntry<Heightmap.Biome> biomes = null!;
        private static ConfigEntry<float> health = null!;
        private static ConfigEntry<int> plates = null!;
        private static ConfigEntry<float> armouredDamage = null!, shatterAt = null!, firePerPlate = null!;
        private static ConfigEntry<float> regrowDelay = null!, regrowInterval = null!;
        private static ConfigEntry<float> sweepDamage = null!, slamDamage = null!, boulderDamage = null!;
        private static ConfigEntry<float> avalancheDamage = null!, avalancheLength = null!;

        /// <summary>Whether new giants may appear. Ones already in the world stay.</summary>
        public static bool On => enabled.Value;

        /// <summary>The share of mountains that hold a giant, 0 to 1; fixed per mountain by the world seed.</summary>
        public static float Mountains => mountains.Value / 100f;

        /// <summary>Percent per zone roll, on a mountain whose giant has not come yet.</summary>
        public static float Chance => chance.Value;

        public static float Interval => interval.Value;
        public static Heightmap.Biome Biomes => biomes.Value;
        public static bool InBiome(Heightmap.Biome biome) => (biomes.Value & biome) != 0;
        public static float Health => health.Value;
        public static int Plates => plates.Value;

        /// <summary>The share of a physical hit that gets through with every plate on, 0 to 1.</summary>
        public static float ArmouredDamage => armouredDamage.Value / 100f;

        /// <summary>The share of its health at which the plates left all break off, 0 to 1.</summary>
        public static float ShatterAt => shatterAt.Value / 100f;

        public static float FirePerPlate => firePerPlate.Value;
        public static float RegrowDelay => regrowDelay.Value;
        public static float RegrowInterval => regrowInterval.Value;
        public static float SweepDamage => sweepDamage.Value;
        public static float SlamDamage => slamDamage.Value;
        public static float AvalancheDamage => avalancheDamage.Value;
        public static float AvalancheLength => avalancheLength.Value;
        public static float BoulderDamage => boulderDamage.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            BindSpawn(config);
            BindArmour(config);
            BindAttacks(config);
        }

        private static void BindSpawn(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "Rime giants: a very rare giant of the Mountains, a troll crusted in plates of ice that sleeps by day like a snowy "
                + "outcrop. At most one per mountain, ever. Off: no new giants; ones already in the world stay.");
            mountains = config.Bind(Section, "Mountains", 60f,
                "Percent of mountains that hold a giant, fixed by the world seed. Snowy hilltops under about 150 m across don't count. "
                + "Once a mountain's giant has come it never gets another, even after it dies.", acceptableValues: Settings.Range(0f, 100f));
            chance = config.Bind(Section, "Chance", 25f,
                "Percent per zone roll, while someone roams a mountain whose giant has not come yet.",
                acceptableValues: Settings.Range(0f, 100f));
            interval = config.Bind(Section, "Interval", 3600f, "Seconds between a zone's rolls.",
                acceptableValues: Settings.Range(60f, 86400f));
            biomes = config.Bind(Section, "Biomes", Heightmap.Biome.Mountain,
                "The biomes whose regions count as mountains. Several are separated by commas.");
            health = config.Bind(Section, "Health", 1500f, "Its health before stars (a forest troll: 600).",
                acceptableValues: Settings.Range(1f, 100000f));
        }

        private static void BindArmour(SyncedConfiguration config)
        {
            plates = config.Bind(Section, "Plates", 8, "Plates of ice it wears, 0 to 8.",
                acceptableValues: new AcceptableValueRange<int>(0, 8));
            armouredDamage = config.Bind(Section, "Armoured Damage", 15f,
                "Percent of a physical hit that gets through with every plate on; each plate lost lets an equal part more through.",
                acceptableValues: Settings.Range(0f, 100f));
            shatterAt = config.Bind(Section, "Shatter At", 40f,
                "Percent of its health at which the plates left all break off.", acceptableValues: Settings.Range(0f, 100f));
            firePerPlate = config.Bind(Section, "Fire Per Plate", 30f,
                "Fire damage that breaks one plate (a fire arrow against its weakness: about 33).",
                acceptableValues: Settings.Range(1f, 1000f));
            regrowDelay = config.Bind(Section, "Regrow Delay", 12f,
                "Seconds unhurt, out of the fight, before the plates grow back.", acceptableValues: Settings.Range(0f, 600f));
            regrowInterval = config.Bind(Section, "Regrow Interval", 5f, "Seconds per plate as they grow back.",
                acceptableValues: Settings.Range(0.5f, 600f));
        }

        private static void BindAttacks(SyncedConfiguration config)
        {
            sweepDamage = config.Bind(Section, "Sweep Damage", 75f, "Blunt damage of its fist, with heavy knockback (a troll's punch: 60).",
                acceptableValues: Settings.Range(0f, 1000f));
            slamDamage = config.Bind(Section, "Slam Damage", 60f, "Blunt damage where its fists land in the slam.",
                acceptableValues: Settings.Range(0f, 1000f));
            avalancheDamage = config.Bind(Section, "Avalanche Damage", 45f,
                "Blunt damage, plus half as much frost, where the slam's wave rolls over you.", acceptableValues: Settings.Range(0f, 1000f));
            avalancheLength = config.Bind(Section, "Avalanche Length", 24f,
                "Metres the wave rolls on flat ground; far further downhill, a few metres uphill. 0 turns the avalanche off.",
                acceptableValues: Settings.Range(0f, 60f));
            boulderDamage = config.Bind(Section, "Boulder Damage", 40f,
                "Blunt damage of its ice boulder, plus as much frost in the burst around it.", acceptableValues: Settings.Range(0f, 1000f));
        }
    }
}
