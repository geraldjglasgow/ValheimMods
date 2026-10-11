using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// What a creature carries, as its Humanoid lists it: the items it always has (<c>m_defaultItems</c>), one from each of
    /// the random weapon, armour and shield lists, one of the random sets and the chance items. A creature's attacks are
    /// items among them. The game hands them out in <c>Humanoid.GiveDefaultItems</c> as each creature starts, on every
    /// peer, rolled from the creature's seed (its ZDO), so every peer arms it alike from the same prefab.
    /// <para>
    /// The lists are only ever replaced, never written into: a new array, new sets, new chance items. So whatever the
    /// shell shares with another prefab, the other prefab is left alone.
    /// </para>
    /// </summary>
    internal static class CarriedItems
    {
        /// <summary>Every item prefab the creature can be given, each once, in the lists' order (copies are named in this
        /// order, which is the same on every peer).</summary>
        public static List<GameObject> All(Humanoid humanoid)
        {
            List<GameObject> all = new List<GameObject>();
            AddEach(all, humanoid.m_defaultItems);
            AddEach(all, humanoid.m_randomWeapon);
            AddEach(all, humanoid.m_randomArmor);
            AddEach(all, humanoid.m_randomShield);
            foreach (Humanoid.ItemSet set in humanoid.m_randomSets ?? Array.Empty<Humanoid.ItemSet>())
            {
                AddEach(all, set.m_items);
            }
            foreach (Humanoid.RandomItem item in humanoid.m_randomItems ?? Array.Empty<Humanoid.RandomItem>())
            {
                AddEach(all, new[] { item.m_prefab });
            }
            return all;
        }

        /// <summary>Puts <paramref name="own"/> wherever the creature's lists name <paramref name="item"/>.</summary>
        public static void Swap(Humanoid humanoid, GameObject item, GameObject own)
        {
            humanoid.m_defaultItems = Swapped(humanoid.m_defaultItems, item, own);
            humanoid.m_randomWeapon = Swapped(humanoid.m_randomWeapon, item, own);
            humanoid.m_randomArmor = Swapped(humanoid.m_randomArmor, item, own);
            humanoid.m_randomShield = Swapped(humanoid.m_randomShield, item, own);
            humanoid.m_randomSets = (humanoid.m_randomSets ?? Array.Empty<Humanoid.ItemSet>())
                .Select(set => new Humanoid.ItemSet { m_name = set.m_name, m_items = Swapped(set.m_items, item, own) }).ToArray();
            humanoid.m_randomItems = (humanoid.m_randomItems ?? Array.Empty<Humanoid.RandomItem>())
                .Select(chance => new Humanoid.RandomItem { m_prefab = chance.m_prefab == item ? own : chance.m_prefab, m_chance = chance.m_chance })
                .ToArray();
        }

        /// <summary>Empties every list: the creature carries nothing of its base's (its unarmed weapon stays).</summary>
        public static void Clear(Humanoid humanoid)
        {
            humanoid.m_defaultItems = new GameObject[0];
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomArmor = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
        }

        /// <summary>Adds items to the ones the creature always carries, after its own.</summary>
        public static void Always(Humanoid humanoid, IEnumerable<GameObject> items) =>
            humanoid.m_defaultItems = (humanoid.m_defaultItems ?? new GameObject[0]).Concat(items).ToArray();

        /// <summary>Whether the item is one the AI can attack with (the game's own test: a weapon, a bow or a torch).</summary>
        public static bool IsAttack(GameObject item)
        {
            ItemDrop? drop = item.GetComponent<ItemDrop>();
            return drop != null && drop.m_itemData.m_shared != null && drop.m_itemData.IsWeapon();
        }

        private static GameObject[] Swapped(GameObject[]? list, GameObject item, GameObject own) =>
            (list ?? new GameObject[0]).Select(entry => entry == item ? own : entry).ToArray();

        private static void AddEach(List<GameObject> all, GameObject[]? items)
        {
            foreach (GameObject item in items ?? Array.Empty<GameObject>())
            {
                if (item != null && !all.Contains(item))
                {
                    all.Add(item);
                }
            }
        }
    }
}
