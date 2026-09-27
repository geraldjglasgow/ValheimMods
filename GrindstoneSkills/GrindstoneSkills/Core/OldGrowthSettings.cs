using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Old growth (section 12, Chopping): more wood from the logs of the biggest trees of each kind. A tree's size is
    /// measured within its own kind's range, 0% for the smallest it grows to and 100% for the largest; the bonus grows
    /// from nothing at the start to all of it at the full size, and linearly with the woodcutter's level. Synced.
    /// </summary>
    public static class OldGrowthSettings
    {
        public const string Section = WoodcuttingSettings.ChoppingSection;

        public static ConfigEntry<float> Bonus { get; private set; }
        public static ConfigEntry<float> Start { get; private set; }
        public static ConfigEntry<float> Full { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Bonus = config.Bind(Section, "Old Growth Bonus", 100f,
                "Percent more wood from the logs of the biggest trees for a level 100 woodcutter, less for smaller trees and lower levels. It follows the level of whoever breaks the log, or else of whoever felled the tree.",
                acceptableValues: Settings.UpTo(500f));
            Start = config.Bind(Section, "Old Growth Start", 50f,
                "How big a tree must be before its logs give extra wood, in percent of its own kind's size range: 0 is the smallest size that kind of tree grows to, 100 the largest. Trees this size or smaller give no bonus.",
                acceptableValues: Settings.UpTo(100f));
            Full = config.Bind(Section, "Old Growth Full", 100f,
                "Trees this big within their kind, in percent, give the whole bonus, and trees between the start and this size a share of it. If this is not above the start, every tree larger than the start gives the whole bonus.",
                acceptableValues: Settings.UpTo(100f));
        }
    }
}
