using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// The backpack a player wears (the user's request, 2026-09-28, then one per biome): an item of PackPanel's own, worn by
    /// lying in the Backpack slot. While it lies there the main grid has its slots at the bottom (<see cref="LayoutBuilder"/>,
    /// a partly used last row keeps its spare cells blocked), the player carries more (<see cref="CarryWeight"/>), and the
    /// player's ZDO names it, so every client hangs its model on that player's back (<see cref="BackpackMount"/>). The game
    /// knows no equip state for it: it is a Misc item, worn by where it lies.
    /// </summary>
    public static class Backpack
    {
        /// <summary>On a player's ZDO (int): the stable hash of the worn pack's prefab name, 0 for none. Written by its own client.</summary>
        public static readonly int WornKey = "PackPanel.backpack".GetStableHashCode();

        /// <summary>The kind of the backpack in the Backpack slot of a layout, or null.</summary>
        public static BackpackKind InSlot(Inventory inventory, InventoryLayout layout)
        {
            IReadOnlyList<Vector2i> cells = layout.CellsOf(SlotKind.Backpack);
            if (cells.Count == 0)
                return null;
            return BackpackCatalog.Of(inventory.GetItemAt(cells[0].x, cells[0].y));
        }

        /// <summary>The kind the local player wears now, or null (also while PackPanel or the backpacks are off).</summary>
        public static BackpackKind Worn(Player player)
        {
            if (!InventoryState.IsLocal(player) || !InventoryState.Active || !BackpackSettings.Active)
                return null;
            return InSlot(player.GetInventory(), InventoryState.Layout);
        }

        /// <summary>
        /// The slots the worn pack adds to the next layout. The items still lie where the recorded layout put them, so
        /// the Backpack slot is looked up there.
        /// </summary>
        public static int SlotsFor(Player player) => BackpackSettings.Slots(InSlot(player.GetInventory(), LayoutRecord.Read(player)));
    }
}
