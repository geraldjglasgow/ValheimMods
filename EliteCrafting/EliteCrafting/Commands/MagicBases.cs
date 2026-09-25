using System;
using System.Collections.Generic;
using EliteCrafting.Items;
using EliteCrafting.Loot;
using UnityEngine;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// Item prefabs for <c>ecraft roll</c>: a named prefab (exact, then case-insensitive), or a random magic base of an
    /// affix slot. The random pick is from the gear drop pool (<see cref="GearPool.Bases"/>, drops.md section 8), so a
    /// slot roll samples what could drop; a slot with no drop-eligible base falls back to every magic base of the slot.
    /// Reads the local object database; command path only.
    /// </summary>
    internal static class MagicBases
    {
        public static GameObject? Resolve(string arg, System.Random random, out string? problem)
        {
            if (ItemSlots.TryParse(arg.ToLowerInvariant(), out ItemSlot slot))
            {
                GameObject? picked = RandomOfSlot(slot, random);
                problem = picked == null ? $"no magic base of slot {ItemSlots.Id(slot)} in the object database." : null;
                return picked;
            }
            GameObject? prefab = arg.Length == 0 ? null : FindPrefab(arg);
            problem = arg.Length == 0 ? "which prefab or slot?"
                : prefab == null ? $"'{arg}' is neither an item prefab nor a slot ({SlotIds()})."
                : !ItemSlots.IsMagicBase(prefab.GetComponent<ItemDrop>().m_itemData) ? $"{prefab.name} is not a magic base (stackable, no slot, or a stone)."
                : null;
            return problem == null ? prefab : null;
        }

        private static GameObject? FindPrefab(string name)
        {
            ObjectDB? db = ObjectDB.instance;
            if (db == null)
            {
                return null;
            }
            GameObject? exact = db.GetItemPrefab(name);
            if (exact != null && exact.GetComponent<ItemDrop>() != null)
            {
                return exact;
            }
            return db.m_items.Find(go => go != null && go.GetComponent<ItemDrop>() != null
                && string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase));
        }

        // The drop pool's bases of the slot (Loot's GearPool: same exclusions as a drop), else any magic base of it.
        private static GameObject? RandomOfSlot(ItemSlot slot, System.Random random)
        {
            List<GameObject> pool = new List<GameObject>();
            foreach (GearBase gear in GearPool.Bases)
            {
                if (gear.Slot.Slot == slot)
                {
                    pool.Add(gear.Prefab);
                }
            }
            if (pool.Count == 0)
            {
                AnyOfSlot(slot, pool);
            }
            return pool.Count == 0 ? null : pool[random.Next(pool.Count)];
        }

        private static void AnyOfSlot(ItemSlot slot, List<GameObject> into)
        {
            foreach (GameObject go in ObjectDB.instance?.m_items ?? new List<GameObject>())
            {
                ItemDrop? drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop != null && ItemSlots.IsMagicBase(drop.m_itemData) && ItemSlots.SlotOf(drop.m_itemData) == slot)
                {
                    into.Add(go!);
                }
            }
        }

        private static string SlotIds()
        {
            List<string> ids = new List<string>();
            foreach (ItemSlot slot in (ItemSlot[])Enum.GetValues(typeof(ItemSlot)))
            {
                if (slot != ItemSlot.None)
                {
                    ids.Add(ItemSlots.Id(slot));
                }
            }
            return string.Join(", ", ids);
        }
    }
}
