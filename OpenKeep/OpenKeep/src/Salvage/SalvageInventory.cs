using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// Fitting and adding salvage returns to an inventory. <see cref="Fits"/> is an exact simulation of the game's
    /// add rules (partial stacks of quality 1 at the current world level fill first, the rest needs empty slots)
    /// with the salvaged stack's own slot counted as free. <see cref="Add"/> adds stack by stack with the game's
    /// own AddItem and rolls back everything it added when one add fails, so nothing is ever lost.
    /// </summary>
    public static class SalvageInventory
    {
        public static bool Fits(Inventory inventory, ItemDrop.ItemData salvaged, List<SalvageReturn> returns)
        {
            int freeSlots = inventory.GetEmptySlots() + (inventory.ContainsItem(salvaged) ? 1 : 0);
            int slotsNeeded = 0;
            foreach (SalvageReturn entry in returns)
            {
                int rest = entry.Amount - FreeStackRoom(inventory, entry.Name, salvaged);
                if (rest > 0)
                    slotsNeeded += (rest + entry.MaxStack - 1) / entry.MaxStack;
            }
            return slotsNeeded <= freeSlots;
        }

        /// <summary>Room in partial stacks the game's AddItem would fill: same name, quality 1, current world level.</summary>
        private static int FreeStackRoom(Inventory inventory, string name, ItemDrop.ItemData except)
        {
            int room = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == except || item.m_shared.m_name != name || item.m_quality != 1)
                    continue;
                if (item.m_worldLevel != Game.m_worldLevel || item.m_stack >= item.m_shared.m_maxStackSize)
                    continue;
                room += item.m_shared.m_maxStackSize - item.m_stack;
            }
            return room;
        }

        /// <summary>Adds every return. False when one did not fit; then everything added by this call is removed again.</summary>
        public static bool Add(Inventory inventory, List<SalvageReturn> returns)
        {
            List<KeyValuePair<string, int>> added = new List<KeyValuePair<string, int>>();
            foreach (SalvageReturn entry in returns)
            {
                int count = AddOne(inventory, entry);
                added.Add(new KeyValuePair<string, int>(entry.Name, count));
                if (count < entry.Amount)
                {
                    RollBack(inventory, added);
                    return false;
                }
            }
            return true;
        }

        /// <summary>Adds one material in stacks of its maximum size. Returns how many units landed in the inventory.</summary>
        private static int AddOne(Inventory inventory, SalvageReturn entry)
        {
            int added = 0;
            while (added < entry.Amount)
            {
                int chunk = Mathf.Min(entry.MaxStack, entry.Amount - added);
                ItemDrop.ItemData stack = NewStack(entry.Item, chunk);
                if (inventory.AddItem(stack))
                {
                    added += chunk;
                    continue;
                }
                // The game merges into partial stacks before it fails on the remainder; the remainder stays in the stack.
                return added + (entry.MaxStack > 1 ? chunk - stack.m_stack : 0);
            }
            return added;
        }

        /// <summary>A fresh quality 1 stack of the material at the current world level, the way the game crafts one.</summary>
        public static ItemDrop.ItemData NewStack(ItemDrop drop, int amount)
        {
            ItemDrop.ItemData data = drop.m_itemData.Clone();
            data.m_dropPrefab = drop.gameObject;
            data.m_stack = amount;
            data.m_quality = 1;
            data.m_variant = 0;
            data.m_equipped = false;
            data.m_worldLevel = (byte)Game.m_worldLevel;
            return data;
        }

        private static void RollBack(Inventory inventory, List<KeyValuePair<string, int>> added)
        {
            foreach (KeyValuePair<string, int> entry in added)
            {
                if (entry.Value > 0)
                    inventory.RemoveItem(entry.Key, entry.Value, 1);
            }
        }
    }
}
