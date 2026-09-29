using System.Collections.Generic;
using PackPanel.Ring;
using PackPanel.Tackle;
using PackPanel.Slots;

namespace PackPanel.Layout
{
    /// <summary>
    /// The main grid's free cells, the game's rules limited to it: the game looks for room over the whole inventory,
    /// which now includes the slot rows, so a picked up stone would land in the helmet slot. Stacking onto a partial
    /// stack still reaches the slots (arrows join the ammo slot's arrows), as the game's own search does.
    /// </summary>
    public static class MainCells
    {
        /// <summary>The first free main cell, top row first or bottom row first as the game asks; (-1, -1) when full.</summary>
        public static Vector2i FindEmpty(Inventory inventory, InventoryLayout layout, bool topFirst)
        {
            for (int i = 0; i < layout.MainRows; i++)
            {
                int y = topFirst ? i : layout.MainRows - 1 - i;
                for (int x = 0; x < layout.Width; x++)
                {
                    if (inventory.GetItemAt(x, y) == null && !layout.IsBlocked(new Vector2i(x, y)))
                        return new Vector2i(x, y);
                }
            }
            return new Vector2i(-1, -1);
        }

        public static int CountEmpty(Inventory inventory, InventoryLayout layout)
        {
            int used = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (layout.IsMain(item.m_gridPos))
                    used++;
            }
            return layout.Width * layout.MainRows - layout.BlockedCells - used;
        }

        /// <summary>The game's CanAddItem over the main grid: room on partial stacks plus whole free cells.</summary>
        public static bool CanAdd(Inventory inventory, InventoryLayout layout, ItemDrop.ItemData item, int stack)
        {
            if (stack <= 0)
                stack = item.m_stack;
            int room = inventory.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) + PurseRoom(inventory, layout, item)
                + KeyRoom(inventory, layout, item) + TackleRoom(inventory, layout, item) + AmmoRoom(inventory, layout, item);
            return room + CountEmpty(inventory, layout) * item.m_shared.m_maxStackSize >= stack;
        }

        /// <summary>Bait also fits into the tacklebox's empty cells, so a full grid still picks it up.</summary>
        private static int TackleRoom(Inventory inventory, InventoryLayout layout, ItemDrop.ItemData item) =>
            TackleRules.IsTackle(item) ? SlotFill.EmptyRoom(inventory, layout.CellsOf(SlotKind.Tackle), item) : 0;

        /// <summary>Arrows and bolts also fit into the empty Ammo slots (<see cref="AmmoRouting"/>), so a full grid still picks them up and crafts them.</summary>
        private static int AmmoRoom(Inventory inventory, InventoryLayout layout, ItemDrop.ItemData item) =>
            AmmoRouting.IsAmmo(item) ? SlotFill.EmptyRoom(inventory, layout.CellsOf(SlotKind.Ammo), item) : 0;

        /// <summary>A key also fits into its empty ring cell, so a full grid still picks it up.</summary>
        private static int KeyRoom(Inventory inventory, InventoryLayout layout, ItemDrop.ItemData item)
        {
            Vector2i ring = KeyRing.CellOf(layout, item);
            return ring.x >= 0 && inventory.GetItemAt(ring.x, ring.y) == null ? item.m_shared.m_maxStackSize : 0;
        }

        /// <summary>Coins also fit into an empty purse (a purse holding coins is counted as a free stack already).</summary>
        private static int PurseRoom(Inventory inventory, InventoryLayout layout, ItemDrop.ItemData item)
        {
            if (!SlotRules.IsCoins(item))
                return 0;
            foreach (Vector2i cell in layout.CellsOf(SlotKind.Purse))
            {
                if (inventory.GetItemAt(cell.x, cell.y) == null)
                    return item.m_shared.m_maxStackSize;
            }
            return 0;
        }
    }
}
