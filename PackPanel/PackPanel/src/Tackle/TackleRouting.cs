using System;
using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Slots;

namespace PackPanel.Tackle
{
    /// <summary>
    /// Bait added to the player's inventory without a cell (pickups, a trader, a click move from a chest) goes into the
    /// tacklebox first, the way keys go to the ring and coins to the purse: onto a stack of the same bait in the box up to
    /// the stack size, then into an empty cell whole. What the box cannot hold goes on through the game's own add (another
    /// stack, then a free main cell). A take all is sorted out after it (<see cref="TakeAllRouting"/>).
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) })]
    public static class TackleRouting
    {
        /// <summary>An add the purse or the key ring already made (an item named in Tackle Items could be either) is left alone.</summary>
        [HarmonyPrefix]
        public static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result, bool __runOriginal)
        {
            if (!__runOriginal)
                return false;
            if (item == null || !InventoryState.Manages(__instance) || !Tacklebox.Active || !TackleRules.IsTackle(item) || !TakeIn(__instance, item))
                return true;
            __result = true;
            return false;
        }

        /// <summary>
        /// Moves what fits of an item into the box; true when none is left outside it. The item is either not in the
        /// inventory yet (an add) or in a main cell (a take all): an empty cell takes it whole, as the same item.
        /// </summary>
        public static bool TakeIn(Inventory inventory, ItemDrop.ItemData item)
        {
            IReadOnlyList<Vector2i> cells = InventoryState.CellsOf(SlotKind.Tackle);
            foreach (Vector2i cell in cells)
            {
                if (Stack(inventory, item, cell))
                    return true;
            }
            foreach (Vector2i cell in cells)
            {
                if (inventory.GetItemAt(cell.x, cell.y) == null)
                {
                    Place(inventory, item, cell);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Onto the stack in a cell when it is the same bait (quality, so stars, included); true when all of it went.</summary>
        private static bool Stack(Inventory inventory, ItemDrop.ItemData item, Vector2i cell)
        {
            ItemDrop.ItemData there = inventory.GetItemAt(cell.x, cell.y);
            if (there == null || there == item || !there.IsSameType(item) || there.m_quality != item.m_quality)
                return false;
            int moved = Math.Min(there.m_shared.m_maxStackSize - there.m_stack, item.m_stack);
            if (moved <= 0)
                return false;
            there.m_stack += moved;
            item.m_stack -= moved;
            inventory.Changed();
            return item.m_stack <= 0;
        }

        private static void Place(Inventory inventory, ItemDrop.ItemData item, Vector2i cell)
        {
            item.m_gridPos = cell;
            if (!inventory.ContainsItem(item))
                inventory.m_inventory.Add(item);
            inventory.Changed();
        }
    }
}
