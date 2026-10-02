using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Layout;
using PackPanel.Slots;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// Right click on one of PackPanel's backpacks in the player's own inventory wears it or takes it off, as the game does
    /// for armour: from the grid it goes into the Backpack slot (what lay there, another pack, takes its cell), from the
    /// slot into a free cell of the grid above the backpack's own rows, which go with it. The game itself would do nothing:
    /// a backpack is a Misc item. Only from the inventory screen; a hotbar key still offers it to what the player looks at.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    public static class BackpackUse
    {
        [HarmonyPrefix]
        public static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
        {
            if (!fromInventoryGui || BackpackCatalog.Of(item) == null || !InventoryState.IsLocal(__instance))
                return true;
            Inventory own = inventory ?? __instance.GetInventory();
            IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(SlotKind.Backpack);
            if (!InventoryState.Manages(own) || cells.Count == 0 || !own.ContainsItem(item))
                return true;
            if (item.m_gridPos == cells[0])
                TakeOff(own, item);
            else
                PutOn(own, item, cells[0]);
            return false;
        }

        private static void PutOn(Inventory inventory, ItemDrop.ItemData item, Vector2i slot)
        {
            ItemDrop.ItemData there = inventory.GetItemAt(slot.x, slot.y);
            if (there != null)
                there.m_gridPos = item.m_gridPos;
            item.m_gridPos = slot;
            inventory.Changed();
        }

        /// <summary>Bottom row first, as the game places what it picks up, skipping the cells that leave with the backpack.</summary>
        private static void TakeOff(Inventory inventory, ItemDrop.ItemData item)
        {
            InventoryLayout layout = InventoryState.Layout;
            for (int y = layout.MainRows - 1; y >= 0; y--)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    Vector2i cell = new Vector2i(x, y);
                    if (!layout.IsMain(cell) || layout.IsPackCell(cell) || inventory.GetItemAt(x, y) != null)
                        continue;
                    item.m_gridPos = cell;
                    inventory.Changed();
                    return;
                }
            }
            Messages.Center(BackpackWords.NoRoom);
        }
    }
}
