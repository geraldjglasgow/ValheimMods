using BepInEx.Configuration;
using EliteCreaturesPack.Core;
using SyncedConfig;

namespace EliteCreaturesPack.Slinger
{
    /// <summary>
    /// Section 3: the Greydwarf Slinger. Whether greydwarfs can come as slingers, what share of them, where (the
    /// biomes, and whether nests spawn them too), and the numbers of its fight that are not the Greydwarf's own - how
    /// often it shoots, how hard and fast a stone flies and how far it shoots from. Everything else (health,
    /// resistances, faction, loot) is the Greydwarf's. Synced; read where they apply, so a reload takes effect.
    /// </summary>
    public static class SlingerSettings
    {
        public const string Section = "3 - Greydwarf Slinger";

        private static ConfigEntry<bool> enabled = null!;
        private static ConfigEntry<float> share = null!;
        private static ConfigEntry<bool> nests = null!;
        private static ConfigEntry<Heightmap.Biome> biomes = null!;
        private static ConfigEntry<float> shotInterval = null!;
        private static ConfigEntry<float> stoneDamage = null!;
        private static ConfigEntry<float> stoneSpeed = null!;
        private static ConfigEntry<float> range = null!;

        /// <summary>Whether new slingers may appear. Ones already in the world stay.</summary>
        public static bool On => enabled.Value;

        /// <summary>The share of the greydwarfs spawning in the biomes that are slingers, 0 to 1.</summary>
        public static float Share => share.Value / 100f;

        public static bool Nests => nests.Value;
        public static bool InBiome(Heightmap.Biome biome) => (biomes.Value & biome) != 0;
        public static float ShotInterval => shotInterval.Value;
        public static float StoneDamage => stoneDamage.Value;
        public static float StoneSpeed => stoneSpeed.Value;
        public static float Range => range.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "Greydwarf slingers: a greydwarf with a slingshot that stands and shoots stones like a skeleton archer, "
                + "at any range, and never melees. Off: no new slingers; ones already in the world stay.");
            share = config.Bind(Section, "Share", 10f,
                "Percent of the greydwarfs spawning in the biomes below that come as slingers instead.",
                acceptableValues: Settings.Range(0f, 100f));
            nests = config.Bind(Section, "Nests", true, "Greydwarf nests spawn them too, not only the wild.");
            biomes = config.Bind(Section, "Biomes", Heightmap.Biome.BlackForest,
                "Where greydwarfs can come as slingers. Several biomes are separated by commas, like BlackForest, Meadows.");
            BindFight(config);
        }

        private static void BindFight(SyncedConfiguration config)
        {
            shotInterval = config.Bind(Section, "Shot Interval", 3.5f, "Seconds between its shots at the least.",
                acceptableValues: Settings.Range(0.5f, 60f));
            stoneDamage = config.Bind(Section, "Stone Damage", 12f,
                "Blunt damage of a stone, before stars (a greydwarf's thrown rock: 10).", acceptableValues: Settings.Range(0f, 500f));
            stoneSpeed = config.Bind(Section, "Stone Speed", 16f,
                "Metres a second the stone flies (a greydwarf's thrown rock: 12); it arcs onto its target.",
                acceptableValues: Settings.Range(5f, 60f));
            range = config.Bind(Section, "Range", 22f, "Metres it shoots from.", acceptableValues: Settings.Range(6f, 60f));
        }
    }
}
