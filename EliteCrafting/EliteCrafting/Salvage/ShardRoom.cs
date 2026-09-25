using System;
using System.Collections.Generic;
using EliteCrafting.Items;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// Inventory room for what grinding and fusing add (salvage.md sections 3 and 6): partial stacks of the same item
    /// count, and so does the slot the ground item or the used-up shard stack frees. The arithmetic is pure; the
    /// inventory reads use the game's own stacking rule (same name, same world level).
    /// </summary>
    internal static class ShardRoom
    {
        /// <summary>Free slots <paramref name="amount"/> needs after the partial stacks took what they can.</summary>
        public static int SlotsNeeded(int amount, int partialRoom, int maxStack)
        {
            maxStack = Math.Max(1, maxStack);
            return amount <= partialRoom ? 0 : (amount - partialRoom + maxStack - 1) / maxStack;
        }

        /// <summary>
        /// How many of <paramref name="wanted"/> fuse sets fit: each consumes <paramref name="fuse"/> shards from a stack of
        /// <paramref name="stack"/> and adds one stone; using the stack up frees its slot. 0 = none fits.
        /// </summary>
        public static int FittingSets(int wanted, int stack, int fuse, int partialRoom, int emptySlots, int maxStack)
        {
            maxStack = Math.Max(1, maxStack);
            for (int sets = wanted; sets > 0; sets--)
            {
                int freed = sets * fuse >= stack ? 1 : 0;
                if (sets <= partialRoom + (emptySlots + freed) * maxStack)
                {
                    return sets;
                }
            }
            return 0;
        }

        /// <summary>Room in partial stacks of this prefab's item, as the game stacks it (name and world level).</summary>
        public static int PartialRoom(Inventory inventory, GameObject prefab) =>
            inventory.FindFreeStackSpace(Shared(prefab).m_name, Game.m_worldLevel);

        public static int MaxStack(GameObject prefab) => Math.Max(1, Shared(prefab).m_maxStackSize);

        /// <summary>Whether every row's shards fit, counting every chance row as a success, once the ground item is gone.</summary>
        public static bool Fits(Inventory inventory, Dictionary<string, int> totals)
        {
            int slots = 0;
            foreach (KeyValuePair<string, int> kind in totals)
            {
                GameObject? prefab = StonePrefabs.GetShard(kind.Key);
                if (prefab == null)
                {
                    return false;
                }
                slots += SlotsNeeded(kind.Value, PartialRoom(inventory, prefab), MaxStack(prefab));
            }
            return slots <= inventory.GetEmptySlots() + 1;
        }

        /// <summary>Adds <paramref name="amount"/> of a stackable prefab in stack-size chunks (the game's own add, world level set).</summary>
        public static void Add(Inventory inventory, GameObject prefab, int amount)
        {
            int max = MaxStack(prefab);
            while (amount > 0)
            {
                int chunk = Math.Min(amount, max);
                if (!inventory.AddItem(prefab, chunk))
                {
                    return;
                }
                amount -= chunk;
            }
        }

        private static ItemDrop.ItemData.SharedData Shared(GameObject prefab) => prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
    }
}
