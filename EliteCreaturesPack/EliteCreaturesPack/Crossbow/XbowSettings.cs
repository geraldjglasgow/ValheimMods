using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// Section 6: the Skeleton Crossbowmen. Whether the game's archer skeletons can come as crossbowmen and what share of
    /// them (they spawn wherever and whenever those skeletons do, like any skeleton unit), and the numbers of the shot
    /// that are not the skeleton's own: how hard it hits against that skeleton's bow, how often it shoots, how fast a
    /// bolt flies and how far it shoots from. Everything else (health, resistances, faction, loot) is its skeleton's.
    /// Synced; read where they apply, so a reload takes effect.
    /// </summary>
    public static class XbowSettings
    {
        public const string Section = "6 - Skeleton Crossbowman";

        private static ConfigEntry<bool> enabled = null!;
        private static ConfigEntry<float> share = null!;
        private static ConfigEntry<float> damageFactor = null!;
        private static ConfigEntry<float> shotInterval = null!;
        private static ConfigEntry<float> boltSpeed = null!;
        private static ConfigEntry<float> range = null!;

        /// <summary>Whether new crossbowmen may appear. Ones already in the world stay.</summary>
        public static bool On => enabled.Value;

        /// <summary>The share of the archer skeletons' spawns that are crossbowmen, 0 to 1.</summary>
        public static float Share => share.Value / 100f;

        public static float DamageFactor => damageFactor.Value;
        public static float ShotInterval => shotInterval.Value;
        public static float BoltSpeed => boltSpeed.Value;
        public static float Range => range.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "Skeleton crossbowmen: skeletons with a crossbow of bones and a quiver of blunt bone bolts that fight like the "
                + "skeleton archer: they raise the crossbow, aim, shoot, then span it and load a bolt. They come in place of "
                + "the game's archer skeletons (the Black Forest's, the Meadows', the Swamps', the Mountains'), wherever and "
                + "whenever those spawn. Off: no new crossbowmen; ones already in the world stay.");
            share = config.Bind(Section, "Share", 15f,
                "Percent of the spawns of those skeletons that come as crossbowmen instead.",
                acceptableValues: Settings.Range(0f, 100f));
            BindShot(config);
        }

        private static void BindShot(SyncedConfiguration config)
        {
            damageFactor = config.Bind(Section, "Damage Factor", 1f,
                "A bolt's damage, as blunt, against the bow of the skeleton it replaces (Black Forest 20, Meadows 15, Swamps 55, "
                + "Mountains 60), before stars. 1: the same as that skeleton's archer.", acceptableValues: Settings.Range(0f, 10f));
            shotInterval = config.Bind(Section, "Shot Interval", 6f,
                "Seconds between its shots at the least (the skeleton archer's: 4). The whole shot, spanning and loading included, takes about 5.5 of them.",
                acceptableValues: Settings.Range(3f, 60f));
            boltSpeed = config.Bind(Section, "Bolt Speed", 40f,
                "Metres a second the bolt flies, straight (the skeleton archer's arrow: 30).", acceptableValues: Settings.Range(10f, 120f));
            range = config.Bind(Section, "Range", 25f, "Metres it shoots from (the skeleton archer: 20).",
                acceptableValues: Settings.Range(6f, 60f));
        }
    }
}
