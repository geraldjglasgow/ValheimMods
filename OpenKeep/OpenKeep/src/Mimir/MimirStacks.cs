using System;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Big stacks in a Mímir's Chest (user, 2026-10-07: "allow stack sizes up to 9999", only in Mímir's Chest): inside a
    /// Mímir inventory (known by its name, <see cref="MimirPrefab.ContainerName"/>) every item that stacks at all holds up
    /// to <see cref="MaxStack"/> units per slot; an item that does not stack stays one per slot. The items' shared data is
    /// never changed: the game's own room and merge steps are answered for Mímir inventories by
    /// <see cref="MimirStacksPatches"/> and <see cref="MimirStacksAdd"/>, and whatever leaves the chest for any other
    /// inventory or the ground goes at most one normal stack at a time (<see cref="MimirStacksOut"/>). The game saves a
    /// stack as a 16-bit number, so 9999 survives the ZDO; every peer loads it without the game's clamp.
    /// </summary>
    public static class MimirStacks
    {
        public const int MaxStack = 9999;

        public static bool IsMimir(Inventory inventory) => inventory != null && inventory.GetName() == MimirPrefab.ContainerName;

        /// <summary>The item stacks at all (its normal stack is more than one).</summary>
        public static bool Stacks(ItemDrop.ItemData item) => item != null && item.m_shared != null && item.m_shared.m_maxStackSize > 1;

        /// <summary>The most units of the item one slot of the inventory holds: 9999 in a Mímir's Chest, else the item's own.</summary>
        public static int Limit(Inventory inventory, ItemDrop.ItemData item)
        {
            int normal = item.m_shared.m_maxStackSize;
            return normal > 1 && IsMimir(inventory) ? Math.Max(normal, MaxStack) : normal;
        }

        /// <summary>The most units of one stack a single move may put into the inventory: all of it into a Mímir's Chest,
        /// one normal stack into any other inventory.</summary>
        public static int MostInto(Inventory inventory, ItemDrop.ItemData item)
        {
            return IsMimir(inventory) || !Stacks(item) ? int.MaxValue : item.m_shared.m_maxStackSize;
        }

        /// <summary>A stack bigger than its item's normal stack: one that came out of a Mímir's Chest.</summary>
        public static bool Oversized(ItemDrop.ItemData item) => Stacks(item) && item.m_stack > item.m_shared.m_maxStackSize;
    }
}
