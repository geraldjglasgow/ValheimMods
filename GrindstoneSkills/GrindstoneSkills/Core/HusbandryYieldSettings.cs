using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 26: what animals give. Butchering follows the killer's level; produce the best keeper's within Keeper
    /// Range; extra honey the harvester's. Each grows linearly from nothing at Husbandry level 0 to its value at level 100.
    /// Prime Cuts is a switch. Synced.
    /// </summary>
    public static class HusbandryYieldSettings
    {
        public const string Section = HusbandrySettings.YieldSection;

        public static ConfigEntry<float> ButcherYield { get; private set; }
        public static ConfigEntry<bool> PrimeCuts { get; private set; }
        public static ConfigEntry<float> ProduceChance { get; private set; }
        public static ConfigEntry<float> ProduceInterval { get; private set; }
        public static ConfigEntry<float> ExtraHoney { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindButcher(config);
            BindLiving(config);
        }

        private static void BindButcher(SyncedConfiguration config)
        {
            ButcherYield = config.Bind(Section, "Butcher Yield At 100", 50f,
                "Percent more drops from a tamed animal killed by a level 100 player (meat, hides, feathers; never trophies). A fraction is a chance of one more.",
                acceptableValues: Settings.UpTo(500f));
            PrimeCuts = config.Bind(Section, "Prime Cuts", false,
                "When on, meat from a starred tamed animal carries its stars (up to 3) into Cooking, where starred ingredients raise a dish's odds. Meat of different stars then stacks apart. Turning it off takes effect for new meat; items already starred keep their stars until a restart.");
        }

        private static void BindLiving(SyncedConfiguration config)
        {
            ProduceChance = config.Bind(Section, "Produce Chance At 100", 50f,
                "Percent chance, each Produce Interval, that a fed tamed animal with a level 100 keeper near drops one of its own materials without being killed: feathers from hens, leather scraps from boars, pelts from wolves and lox, hides from moose. Meat and trophies never. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
            ProduceInterval = config.Bind(Section, "Produce Interval", 1200f,
                "Seconds between an animal's produce rolls.", acceptableValues: new AcceptableValueRange<float>(60f, 86400f));
            ExtraHoney = config.Bind(Section, "Extra Honey At 100", 50f,
                "Percent chance per honey that a level 100 player harvesting a beehive gets one more. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
        }
    }
}
