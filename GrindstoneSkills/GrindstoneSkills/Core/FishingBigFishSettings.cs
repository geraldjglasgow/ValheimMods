using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 53: big fish. The game rolls a fish's level (1 to 5) when it spawns; a big one grows a level on the hook,
    /// by the angler's level and more at night. A legendary fish is a sixth level the game never rolls: three times the
    /// size, glowing, only biting for skilled anglers, and announced to everyone when landed. Synced.
    /// </summary>
    public static class FishingBigFishSettings
    {
        public const string Section = FishingSettings.BigFishSection;

        public static ConfigEntry<float> BigOneChanceAt100 { get; private set; }
        public static ConfigEntry<float> NightBonus { get; private set; }
        public static ConfigEntry<float> LegendaryChance { get; private set; }
        public static ConfigEntry<float> LegendaryLevel { get; private set; }
        public static ConfigEntry<int> LegendaryThrashes { get; private set; }
        public static ConfigEntry<bool> AnnounceLegendary { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindBigOnes(config);
            BindLegendary(config);
        }

        private static void BindBigOnes(SyncedConfiguration config)
        {
            BigOneChanceAt100 = config.Bind(Section, "Big One Chance At 100", 25f,
                "Percent chance, for a level 100 angler, that a fish grows a level when it is hooked (It's a big one!). Rolled again for each further level, up to level 5.",
                acceptableValues: Settings.UpTo(100f));
            NightBonus = config.Bind(Section, "Night Big One Bonus", 50f,
                "Percent more big-one chance at night.", acceptableValues: Settings.UpTo(500f));
        }

        private static void BindLegendary(SyncedConfiguration config)
        {
            LegendaryChance = config.Bind(Section, "Legendary Chance", 0.5f,
                "Percent of fish spawned in the world that are legendary: level 6, three times the size, glowing. Fish dropped back in the water never become legendary. 0 turns new legendary fish off.",
                acceptableValues: Settings.UpTo(100f));
            LegendaryLevel = config.Bind(Section, "Legendary Level", 50f,
                "Fishing level from which a legendary fish takes your bait. 0 lets everyone hook them; above 100 nobody can.",
                acceptableValues: new AcceptableValueRange<float>(0f, 101f));
            LegendaryThrashes = config.Bind(Section, "Legendary Thrashes", 8,
                "Thrashes after which a hooked legendary fish is spent. 0 means it never tires.",
                acceptableValues: new AcceptableValueRange<int>(0, 50));
            AnnounceLegendary = config.Bind(Section, "Announce Legendary Catches", true,
                "Tells every player on the server when someone lands a legendary fish.");
        }
    }
}
