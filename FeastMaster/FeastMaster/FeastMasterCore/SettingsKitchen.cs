using BepInEx.Configuration;
using SyncedConfig;

namespace FeastMaster
{
    /// <summary>Section 9: the fermenter. Synced, and read whenever a barrel is checked or tapped.</summary>
    public static partial class Settings
    {
        // 9. Fermenter
        public static ConfigEntry<float> FermentationTime { get; private set; }
        public static ConfigEntry<int> BatchYield { get; private set; }

        private static void BindFermenter(SyncedConfiguration config)
        {
            FermentationTime = config.Bind(FermenterSection, "Fermentation Time", 0f,
                "Seconds a fermenter takes to brew a batch. 0 keeps each barrel's own time (the game's fermenter takes 2400, 40 minutes). Also applies to barrels already brewing: one that has brewed longer than the new time is ready at once.",
                acceptableValues: new AcceptableValueRange<float>(0f, 86400f));
            BatchYield = config.Bind(FermenterSection, "Batch Yield", 0,
                "Meads tapped from one batch. 0 keeps each recipe's own yield. Applies when a batch is tapped, so it also covers batches already brewing.",
                acceptableValues: new AcceptableValueRange<int>(0, 100));
        }
    }
}
