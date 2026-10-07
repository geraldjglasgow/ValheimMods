using System;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The game's add steps for a Mímir inventory, with <see cref="MimirStacks.Limit"/> in place of the item's normal
    /// stack. <see cref="Add"/> is <c>Inventory.AddItem(item)</c> (and its slot variant): the units first join the stacks
    /// of the same name, quality and world level that still have room, all at once rather than the game's one by one, the
    /// rest becomes a new stack in the first empty cell (top-first, <see cref="MimirFill"/>) or the given cell. Like the
    /// game, a full join leaves the item's own count as it was, and a failed add leaves it at what is left over.
    /// <see cref="TryMerge"/> is the game's drop onto an occupied cell (drag and drop, MoveItemToThis, MoveAll).
    /// Runs wherever the game changes the chest: on its owner, the opener.
    /// </summary>
    public static class MimirStacksAdd
    {
        public static bool Add(Inventory inventory, ItemDrop.ItemData item, Vector2i? cell)
        {
            bool cheatedChanged = item.m_cheated && !Achievements.IsCheatedAtAll();
            int before = item.m_stack;
            bool added;
            if (MimirStacks.Stacks(item) && Join(inventory, item))
            {
                item.m_stack = before;
                added = true;
            }
            else
                added = Place(inventory, item, cell);
            inventory.Changed(added, cheatedChanged);
            return added;
        }

        /// <summary>Pours the units into the stacks of the same kind with room; true when none are left.</summary>
        private static bool Join(Inventory inventory, ItemDrop.ItemData item)
        {
            foreach (ItemDrop.ItemData stack in inventory.GetAllItems())
            {
                if (item.m_stack <= 0)
                    break;
                if (!Joins(inventory, stack, item))
                    continue;
                int part = Math.Min(MimirStacks.Limit(inventory, stack) - stack.m_stack, item.m_stack);
                stack.m_stack += part;
                item.m_stack -= part;
                if (item.m_cheated && !PlayerProfile.s_bypassCheatChecks)
                    stack.m_cheated = true;
            }
            return item.m_stack <= 0;
        }

        private static bool Joins(Inventory inventory, ItemDrop.ItemData stack, ItemDrop.ItemData item)
        {
            return stack != item && stack.m_shared.m_name == item.m_shared.m_name && stack.m_quality == item.m_quality
                && stack.m_worldLevel == item.m_worldLevel && stack.m_stack < MimirStacks.Limit(inventory, stack);
        }

        /// <summary>The item itself takes the cell, or the first empty one.</summary>
        private static bool Place(Inventory inventory, ItemDrop.ItemData item, Vector2i? cell)
        {
            Vector2i at = cell ?? inventory.FindEmptySlot(inventory.TopFirst(item));
            if (at.x < 0 || inventory.GetItemAt(at.x, at.y) != null)
            {
                ZLog.LogWarning($"Trying to add item to occupied slot {at.x}, {at.y}");
                return false;
            }
            item.m_gridPos = at;
            inventory.GetAllItems().Add(item);
            return true;
        }

        /// <summary>
        /// The game's private add of <paramref name="amount"/> units onto the cell (x, y): when that cell holds the same
        /// kind, they join it up to the Mímir limit and true is returned with the add's result; otherwise false, and the
        /// game's own step runs (an empty cell takes any amount, a different item refuses).
        /// </summary>
        public static bool TryMerge(Inventory inventory, ItemDrop.ItemData item, int amount, int x, int y, bool anyRow, out bool result)
        {
            result = false;
            if (x < 0 || y < 0 || x >= inventory.GetWidth() || (y >= inventory.GetHeight() && !anyRow))
                return false;
            ItemDrop.ItemData at = inventory.GetItemAt(x, y);
            if (at == null || !at.IsSameType(item))
                return false;
            amount = Math.Min(amount, item.m_stack);
            int part = Math.Min(MimirStacks.Limit(inventory, at) - at.m_stack, amount);
            if (part <= 0)
                return true;
            at.m_stack += part;
            item.m_stack -= part;
            result = part == amount;
            if (Player.m_localPlayerExists)
                inventory.Changed(true, false);
            return true;
        }
    }
}
