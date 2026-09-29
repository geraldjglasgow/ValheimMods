using System;
using PackPanel.Core;

namespace PackPanel.Slots
{
    /// <summary>
    /// How many slots each group gets. Slots Per Group from 1 to 5 gives Utility, Food, Mead and Ammo that many each (the
    /// user's request: one number for all four); at 0 each group has its own setting, food following the foods a
    /// player can eat (<see cref="FoodCount"/>). Every count is 0 to 5.
    /// </summary>
    public static class SlotCounts
    {
        public static int Utility => Group(InventorySettings.UtilitySlots.Value);
        public static int Food => Shared ? Group(0) : FoodCount.Slots();
        public static int Mead => Group(InventorySettings.MeadSlots.Value);
        public static int Ammo => Group(InventorySettings.AmmoSlots.Value);

        /// <summary>True while Slots Per Group sets all four groups.</summary>
        public static bool Shared => InventorySettings.SlotsPerGroup.Value > 0;

        private static int Group(int own)
        {
            int count = Shared ? InventorySettings.SlotsPerGroup.Value : own;
            return Math.Max(0, Math.Min(InventorySettings.MaxGroup, count));
        }
    }
}
