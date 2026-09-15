using System.Collections.Generic;

namespace OpenKeep.Shared
{
    /// <summary>
    /// A dry run of <c>Inventory.AddItem</c> over a snapshot of an inventory: stackable units first fill the
    /// partial stacks of the same name, quality and world level (the game's stacking rule), the remainder needs
    /// one empty slot; an item that does not stack needs one empty slot. Every <see cref="Reserve"/> books what
    /// it returns, so a series of takes can be checked before any request is sent.
    /// </summary>
    public sealed class InventoryFit
    {
        private readonly Dictionary<string, int> space = new Dictionary<string, int>();
        private int freeSlots;

        public InventoryFit(Inventory inventory)
        {
            freeSlots = inventory.GetEmptySlots();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                int room = item.m_shared.m_maxStackSize - item.m_stack;
                if (room > 0)
                    space[Key(item)] = Room(item) + room;
            }
        }

        /// <summary>How many of <paramref name="amount"/> units of the item fit; those units are booked.</summary>
        public int Reserve(ItemDrop.ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return 0;
            int maxStack = item.m_shared.m_maxStackSize;
            if (maxStack <= 1)
                return TakeSlot() ? 1 : 0;
            string key = Key(item);
            int room = Room(item);
            int merged = room < amount ? room : amount;
            space[key] = room - merged;
            int rest = amount - merged;
            if (rest <= 0)
                return amount;
            if (!TakeSlot())
                return merged;
            space[key] = room - merged + (maxStack - rest);
            return amount;
        }

        private bool TakeSlot()
        {
            if (freeSlots <= 0)
                return false;
            freeSlots--;
            return true;
        }

        private int Room(ItemDrop.ItemData item) => space.TryGetValue(Key(item), out int room) ? room : 0;

        private static string Key(ItemDrop.ItemData item) => item.m_shared.m_name + "|" + item.m_quality + "|" + item.m_worldLevel;
    }
}
