using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// Section 6: the Skeleton Crossbowmen's shot, the numbers that are not its skeleton's own: how hard it hits against
    /// that skeleton's bow, how often it shoots, how fast a bolt flies and how far it shoots from. Everything else
    /// (health, resistances, faction, loot) is its skeleton's. Whether it spawns is an arsenal skeleton's switch
    /// (<see cref="Arsenal.ArsenalSettings.CrossbowmanSpawns"/>; the draw is <see cref="Skeletons.SkeletonDraw"/>).
    /// Synced; read where they apply, so a reload takes effect.
    /// </summary>
    public static class XbowSettings
    {
        public const string Section = "6 - Skeleton Crossbowman";

        private static ConfigEntry<float> damageFactor = null!;
        private static ConfigEntry<float> shotInterval = null!;
        private static ConfigEntry<float> boltSpeed = null!;
        private static ConfigEntry<float> range = null!;

        public static float DamageFactor => damageFactor.Value;
        public static float ShotInterval => shotInterval.Value;
        public static float BoltSpeed => boltSpeed.Value;
        public static float Range => range.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            damageFactor = config.Bind(Section, "Damage Factor", 1f,
                "A bolt's damage, as blunt, against the bow of the skeleton archer it replaces (20), before stars. 1: the "
                + "same as that archer.", acceptableValues: Settings.Range(0f, 10f));
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
