using HarmonyLib;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// What leaves a Mímir's Chest goes one normal stack at a time, so no other inventory and no world drop ever holds a
    /// stack bigger than its item's normal stack (the game would clamp it on the next load and the rest would be lost).
    /// A drag out (or a split) moves at most one normal stack and the rest stays in the chest; a move click, Take all,
    /// Reach, Store and Shared reach the game's add with the big stack, where <see cref="Cap"/> and <see cref="AddPart"/>
    /// let one normal stack's worth in and leave the rest with the source; a drop on the ground drops one normal stack;
    /// a drag that would swap a big stack into an ordinary inventory is refused, since only part of it could go.
    /// </summary>
    public static class MimirStacksOut
    {
        /// <summary>The game's private add onto a cell of an ordinary inventory: the amount is cut to one normal stack.
        /// True when it was cut (the add then reports the move as unfinished).</summary>
        public static bool Cap(ItemDrop.ItemData item, ref int amount)
        {
            int normal = item.m_shared.m_maxStackSize;
            if (normal <= 1 || System.Math.Min(amount, item.m_stack) <= normal)
                return false;
            amount = normal;
            return true;
        }

        /// <summary>
        /// The game's add of a big stack to an ordinary inventory: one normal stack's worth is added the game's way (into
        /// partial stacks first, then a free cell), the item keeps the rest. Always false: the item is not all moved, so
        /// callers leave it where it was (MoveItemToThis, MoveAll and OpenKeep's own moves remove only what arrived).
        /// </summary>
        public static bool AddPart(Inventory inventory, ItemDrop.ItemData item, Vector2i? cell)
        {
            int normal = item.m_shared.m_maxStackSize;
            ItemDrop.ItemData part = item.Clone();
            part.m_stack = normal;
            bool all = cell.HasValue ? inventory.AddItem(part, cell.Value) : inventory.AddItem(part);
            item.m_stack -= all ? normal : normal - part.m_stack;
            return false;
        }

        /// <summary>A whole stack dropped on a different item swaps the two; refused when the stack coming back is too big for the other side.</summary>
        private static bool BlocksSwap(Inventory target, Inventory from, ItemDrop.ItemData item, int amount, Vector2i pos)
        {
            if (target == from || item.m_stack != amount)
                return false;
            ItemDrop.ItemData at = target.GetItemAt(pos.x, pos.y);
            if (at == null || at == item)
                return false;
            bool swaps = at.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && at.m_quality != item.m_quality)
                || at.m_shared.m_maxStackSize == 1;
            return swaps && at.m_stack > MimirStacks.MostInto(from, at);
        }

        /// <summary>Drag and drop between grids: out of a Mímir's Chest at most one normal stack; the drag ends once any of it moved.</summary>
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        private static class DropPatch
        {
            [HarmonyPriority(Priority.High)]
            [HarmonyPrefix]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, ref int amount, Vector2i pos, ref bool __result, out int __state)
            {
                __state = -1;
                Inventory target = __instance.GetInventory();
                if (item == null || fromInventory == null || target == null || amount <= 0)
                    return true;
                if (BlocksSwap(target, fromInventory, item, amount, pos))
                {
                    __result = false;
                    return false;
                }
                if (target != fromInventory && amount > MimirStacks.MostInto(target, item))
                {
                    amount = MimirStacks.MostInto(target, item);
                    __state = item.m_stack;
                }
                return true;
            }

            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData item, int __state, ref bool __result)
            {
                if (__state >= 0 && item != null && item.m_stack < __state)
                    __result = true;
            }
        }

        /// <summary>A drop on the ground from a Mímir's Chest (drag outside, ctrl click, a move with no container) drops one normal stack.</summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
        private static class GroundPatch
        {
            [HarmonyPrefix]
            private static void Prefix(Inventory inventory, ItemDrop.ItemData item, ref int amount)
            {
                if (item != null && MimirStacks.IsMimir(inventory) && MimirStacks.Stacks(item) && amount > item.m_shared.m_maxStackSize)
                    amount = item.m_shared.m_maxStackSize;
            }
        }
    }
}
