using BepInEx.Configuration;
using SyncedConfig;

namespace FeastMaster
{
    /// <summary>
    /// Section 9: the kitchen (fermenter and cooking stations). Synced, and read whenever a barrel is checked or
    /// tapped and on every cooking tick. The per-recipe cook times live in one section per cooking station, see
    /// <see cref="CookTimes"/>.
    /// </summary>
    public static partial class Settings
    {
        /// <summary>The section's name in 4.2.0, before cooking joined it; its entries are carried over.</summary>
        private const string OldFermenterSection = "9. Fermenter";

        // 9. Kitchen
        public static ConfigEntry<float> FermentationTime { get; private set; }
        public static ConfigEntry<int> BatchYield { get; private set; }
        public static ConfigEntry<float> CookTimeMultiplier { get; private set; }
        public static ConfigEntry<bool> FoodCanBurn { get; private set; }

        private static void BindKitchen(SyncedConfiguration config)
        {
            BindFermenter(config);
            CookTimeMultiplier = config.Bind(KitchenSection, "Cook Time Multiplier", 1f,
                "Multiplier on the cook time of every recipe on every cooking station and oven, on top of each recipe's own time in the station's section (one section per station, named after its prefab). 0.5 cooks twice as fast. Food on the fire when the value changes follows at once.",
                acceptableValues: new AcceptableValueRange<float>(0.01f, 100f));
            FoodCanBurn = config.Bind(KitchenSection, "Food Can Burn", true,
                "On: cooked food left on a station burns as in the game, at twice its cook time. Off: cooked food never burns and waits on the station until it is taken.");
        }

        private static void BindFermenter(SyncedConfiguration config)
        {
            FermentationTime = BindMoved(config, "Fermentation Time", 0f,
                "Seconds a fermenter takes to brew a batch. 0 keeps each barrel's own time (the game's fermenter takes 2400, 40 minutes). Also applies to barrels already brewing: one that has brewed longer than the new time is ready at once.",
                new AcceptableValueRange<float>(0f, 86400f));
            BatchYield = BindMoved(config, "Batch Yield", 0,
                "Meads tapped from one batch. 0 keeps each recipe's own yield. Applies when a batch is tapped, so it also covers batches already brewing.",
                new AcceptableValueRange<int>(0, 100));
        }

        /// <summary>Binds an entry of section 9, carrying over its value from the 4.2.0 section name.</summary>
        private static ConfigEntry<T> BindMoved<T>(SyncedConfiguration config, string key, T defaultValue, string description, AcceptableValueBase range)
        {
            string migrated = ConfigMigration.TakeOrphan(config.Config, OldFermenterSection, KitchenSection, key);
            ConfigEntry<T> entry = config.Bind(KitchenSection, key, defaultValue, description, acceptableValues: range);
            ConfigMigration.Apply(entry, migrated);
            return entry;
        }
    }
}
