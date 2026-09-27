using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 54: what a catch brings and the tackle. The fish's own bonus item (the game's 20%) grows with the angler's
    /// level; the bait can come back; a cast that sits long enough can snag something (GrindstoneSkills.Snags.yml says
    /// what); casts and the line reach further; and a cleaned fish's size improves the Cooking stars of its fillets.
    /// Synced, like the file.
    /// </summary>
    public static class FishingCatchSettings
    {
        public const string Section = FishingSettings.CatchSection;

        public static ConfigEntry<float> BonusItemChanceAt100 { get; private set; }
        public static ConfigEntry<float> DoubleBonusLevel { get; private set; }
        public static ConfigEntry<float> BaitSaverAt100 { get; private set; }
        public static ConfigEntry<float> SnagChanceAt0 { get; private set; }
        public static ConfigEntry<float> SnagChanceAt100 { get; private set; }
        public static ConfigEntry<float> SnagWait { get; private set; }
        public static ConfigEntry<float> CastDistanceAt100 { get; private set; }
        public static ConfigEntry<float> LineLengthAt100 { get; private set; }
        public static ConfigEntry<float> FilletLevelsPerFishLevel { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindCatch(config);
            BindSnags(config);
            BindTackle(config);
            SnagFile.Register(config);
        }

        private static void BindCatch(SyncedConfiguration config)
        {
            BonusItemChanceAt100 = config.Bind(Section, "Bonus Item Chance At 100", 40f,
                "Percent chance that a landed fish brings its bonus item (Perch: stone or amber, Tetra: obsidian or coins...) for a level 100 angler. The game's own is 20 at any level; levels in between are in proportion. Legendary fish always bring one.",
                acceptableValues: Settings.UpTo(100f));
            DoubleBonusLevel = config.Bind(Section, "Double Bonus Level", 50f,
                "Fishing level from which a bonus roll can bring two items instead of one. Above 100 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 101f));
            BaitSaverAt100 = config.Bind(Section, "Bait Saver At 100", 30f,
                "Percent chance, for a level 100 angler, that landing a fish gives the bait back. The game already gives it back when nothing bit.",
                acceptableValues: Settings.UpTo(100f));
            FilletLevelsPerFishLevel = config.Bind(Section, "Fillet Levels Per Fish Level", 10f,
                "Cleaning a fish at the prep table rolls Cooking stars for the raw fish it gives. Each level of the fish above 1 adds this many to the cook's level for that roll, like starred ingredients: a level 5 fish +40, a legendary +50. 0 turns it off.",
                acceptableValues: Settings.UpTo(50f));
        }

        private static void BindSnags(SyncedConfiguration config)
        {
            SnagChanceAt0 = config.Bind(Section, "Snag Chance At 0", 2f,
                "Percent chance that a cast left in the water for Snag Wait seconds without a bite snags something from GrindstoneSkills.Snags.yml, for a level 0 angler. It reels in heavy, then lands in your inventory. 0 with the next setting at 0 turns snags off.",
                acceptableValues: Settings.UpTo(100f));
            SnagChanceAt100 = config.Bind(Section, "Snag Chance At 100", 6f,
                "The same for a level 100 angler; levels in between are in proportion.", acceptableValues: Settings.UpTo(100f));
            SnagWait = config.Bind(Section, "Snag Wait", 8f,
                "Seconds a cast must sit in the water, without a fish on the line, before it can snag. Rolled once per cast.",
                acceptableValues: new AcceptableValueRange<float>(1f, 120f));
        }

        private static void BindTackle(SyncedConfiguration config)
        {
            CastDistanceAt100 = config.Bind(Section, "Cast Distance At 100", 30f,
                "Percent farther a level 100 angler casts.", acceptableValues: Settings.UpTo(200f));
            LineLengthAt100 = config.Bind(Section, "Line Length At 100", 50f,
                "Percent longer line for a level 100 angler. The game's line snaps when the float is 30 m from the rod.",
                acceptableValues: Settings.UpTo(200f));
        }
    }
}
