using System.Collections.Generic;
using System;
using PackPanel.Backpacks;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Tackle;
using PackPanel.Slots;

namespace PackPanel.Layout
{
    /// <summary>
    /// The layout the settings ask for. The main grid is Inventory Width wide and Inventory Rows tall plus the rows the
    /// player bought from the trader (the game's <c>invrows</c> key above its 4), plus the rows a worn backpack's slots
    /// need at the bottom (<see cref="Backpack.SlotsFor"/>; a partly used last row keeps its spare cells blocked). The slots follow in a fixed order:
    /// Head, Chest, Legs, Back, Backpack, Utility, Food, Mead, Ammo, Purse, each group as many as <see cref="SlotCounts"/> says,
    /// then with Key Ring on one ring cell per key in Key Items (<see cref="KeyRing"/>), then with Tacklebox on the Tacklebox
    /// slot and as many cells as the box in it gives (<see cref="Tacklebox.CellsFor"/>), last because their count changes
    /// with the box, so no other slot moves when it does. With the module off the layout is
    /// the game's: 8 wide, <c>invrows</c> tall, no slots.
    /// </summary>
    public static class LayoutBuilder
    {
        public static InventoryLayout Wanted(Player player)
        {
            if (!InventorySettings.Enabled.Value)
                return GameLayout(player);
            int rows = InventorySettings.InventoryRows.Value + Math.Max(0, GameRows(player) - InventorySettings.GameRows);
            int width = InventorySettings.InventoryWidth.Value;
            int backpack = Backpack.SlotsFor(player);
            return new InventoryLayout(width, rows + (backpack + width - 1) / width, Slots(player), backpack);
        }

        /// <summary>The game's own layout: what a character without a record has, and what the module off gives.</summary>
        public static InventoryLayout GameLayout(Player player) => new InventoryLayout(InventorySettings.GameWidth, GameRows(player), new List<Slot>());

        /// <summary>The game's row count: its unique key <c>invrows</c>, 4 until set, clamped as the game clamps it.</summary>
        public static int GameRows(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
                return Math.Max(0, Math.Min(9, rows));
            return InventorySettings.GameRows;
        }

        private static List<Slot> Slots(Player player)
        {
            List<Slot> slots = new List<Slot>();
            if (InventorySettings.EquipmentSlots.Value)
                Add(slots, 1, SlotKind.Head, SlotKind.Chest, SlotKind.Legs, SlotKind.Back);
            if (InventorySettings.BackpackSlot.Value)
                Add(slots, 1, SlotKind.Backpack);
            Add(slots, SlotCounts.Utility, SlotKind.Utility);
            Add(slots, SlotCounts.Food, SlotKind.Food);
            Add(slots, SlotCounts.Mead, SlotKind.Mead);
            Add(slots, SlotCounts.Ammo, SlotKind.Ammo);
            if (InventorySettings.CoinPurse.Value)
                Add(slots, 1, SlotKind.Purse);
            if (KeyRing.On)
                Add(slots, KeyRing.Prefabs.Count, SlotKind.Key);
            if (TackleboxSettings.Active)
            {
                Add(slots, 1, SlotKind.Tacklebox);
                Add(slots, Tacklebox.CellsFor(player), SlotKind.Tackle);
            }
            return slots;
        }

        /// <summary>Numbers 1 to <paramref name="count"/> of every kind given.</summary>
        private static void Add(List<Slot> slots, int count, params SlotKind[] kinds)
        {
            foreach (SlotKind kind in kinds)
            {
                for (int number = 1; number <= count; number++)
                    slots.Add(new Slot(kind, number));
            }
        }
    }
}
