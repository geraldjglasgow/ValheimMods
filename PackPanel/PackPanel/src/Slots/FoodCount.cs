using System;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx;
using PackPanel.Core;

namespace PackPanel.Slots
{
    /// <summary>
    /// How many foods a player can eat at once: FeastMaster's Food Slots when that mod is loaded (read from its own
    /// config entry through BepInEx, the value its server synced), the game's 3 otherwise. Looked up on first use,
    /// when every plugin has loaded; a change of FeastMaster's value raises <see cref="Changed"/> so the food slots
    /// follow at once.
    /// </summary>
    public static class FoodCount
    {
        public const int GameFoods = 3;
        private const string FeastMasterGuid = "com.FeastMaster";
        private const string FeastMasterSection = "0. Global Settings";
        private const string FeastMasterKey = "Food Slots";

        private static ConfigEntry<int> feastMaster;
        private static bool looked;

        public static event Action Changed;

        /// <summary>The food slots the layout gets: the eating count or the fixed setting, 0 to 5.</summary>
        public static int Slots()
        {
            int count = InventorySettings.FoodSlotsFollowEating.Value ? Eatable() : InventorySettings.FoodSlots.Value;
            return Math.Max(0, Math.Min(InventorySettings.MaxGroup, count));
        }

        public static int Eatable()
        {
            ConfigEntry<int> entry = FeastMasterEntry();
            return entry != null ? entry.Value : GameFoods;
        }

        private static ConfigEntry<int> FeastMasterEntry()
        {
            if (looked)
                return feastMaster;
            looked = true;
            if (!Chainloader.PluginInfos.TryGetValue(FeastMasterGuid, out PluginInfo info) || info.Instance == null)
                return null;
            if (!info.Instance.Config.TryGetEntry(FeastMasterSection, FeastMasterKey, out feastMaster))
            {
                Plugin.Log.LogWarning($"FeastMaster is loaded but has no {FeastMasterSection} / {FeastMasterKey}; food slots follow the game's {GameFoods} foods");
                return null;
            }
            feastMaster.SettingChanged += (sender, args) => Changed?.Invoke();
            Plugin.Log.LogInfo($"food slots follow FeastMaster's Food Slots ({feastMaster.Value})");
            return feastMaster;
        }
    }
}
