using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 32: the compost bin, a barrel in the cultivator's menu that turns food scraps into compost for nearby
    /// crops. The piece always exists (every machine registers it); Compost Enabled only stops the composting. Synced.
    /// </summary>
    public static class CompostSettings
    {
        public const string Section = FarmingSettings.CompostSection;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> CompostTime { get; private set; }
        public static ConfigEntry<int> Capacity { get; private set; }
        public static ConfigEntry<float> Radius { get; private set; }
        public static ConfigEntry<float> GrowthSpeed { get; private set; }
        public static ConfigEntry<float> StarLevels { get; private set; }
        public static ConfigEntry<bool> KitchenTrash { get; private set; }
        public static ConfigEntry<string> ExtraItems { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindBin(config);
            BindEffect(config);
        }

        private static void BindBin(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Compost Enabled", true,
                "Compost bins turn their contents into compost and feed the crops around them. Off, a bin is a plain barrel.");
            CompostTime = config.Bind(Section, "Compost Time", 30f,
                "Seconds a bin takes to turn one item into one point of compost.", acceptableValues: new AcceptableValueRange<float>(1f, 3600f));
            Capacity = config.Bind(Section, "Compost Capacity", 100,
                "The most compost points a bin holds; it stops composting when full.", acceptableValues: new AcceptableValueRange<int>(1, 10000));
            ExtraItems = config.Bind(Section, "Compost Items", "Entrails, BoneFragments",
                "Item prefab names that compost besides food and anything that carries stars, separated by commas.");
            KitchenTrash = config.Bind(Section, "Kitchen Trash Compost", true,
                "A dish a kitchen's trash filter throws away within 20 m of a compost bin becomes a point of compost in the nearest one.");
        }

        private static void BindEffect(SyncedConfiguration config)
        {
            Radius = config.Bind(Section, "Compost Radius", 12f,
                "A bin fertilizes growing crops within this many metres, one point of compost each.", acceptableValues: new AcceptableValueRange<float>(1f, 50f));
            GrowthSpeed = config.Bind(Section, "Compost Growth Speed", 25f,
                "Percent faster growth for a fertilized crop.", acceptableValues: Settings.UpTo(500f));
            StarLevels = config.Bind(Section, "Compost Star Levels", 10f,
                "Levels added to a fertilized crop's star roll.", acceptableValues: Settings.UpTo(50f));
        }
    }
}
