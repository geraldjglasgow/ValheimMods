using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// The item prefabs a creature can be given - its default items, and its random weapons, shields, sets and items -
    /// read from the creature itself, whose lists are its prefab's. A creature's attacks are items it holds, and those
    /// items are not in the item database, so these lists hold the only copy of them. Portalbound uses this to find an
    /// attack item by name, as an attack starts or a throw is let go, and the API to check a registration: rarely, never
    /// per frame.
    /// </summary>
    internal static class CreatureItems
    {
        /// <summary>The prefab name of the item with this shared name the creature can carry; empty when none.</summary>
        public static string NameOf(Humanoid creature, string sharedName)
        {
            foreach (GameObject prefab in All(creature))
            {
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop != null && drop.m_itemData.m_shared.m_name == sharedName)
                {
                    return prefab.name;
                }
            }
            return "";
        }

        /// <summary>The bones these attack items are let go from (their attack's origin joint), each once.</summary>
        public static string[] Joints(Humanoid creature, string[] items)
        {
            List<string> joints = new List<string>();
            foreach (GameObject prefab in All(creature))
            {
                string joint = Array.IndexOf(items, prefab.name) >= 0 ? OriginJoint(prefab) : "";
                if (joint.Length > 0 && !joints.Contains(joint))
                {
                    joints.Add(joint);
                }
            }
            return joints.ToArray();
        }

        /// <summary>The attack type of the item of this prefab name the creature can be given; null when it has none.</summary>
        public static Attack.AttackType? AttackTypeOf(Humanoid creature, string item)
        {
            foreach (GameObject prefab in All(creature))
            {
                ItemDrop drop = prefab.name == item ? prefab.GetComponent<ItemDrop>() : null!;
                if (drop != null && drop.m_itemData.m_shared.m_attack != null)
                {
                    return drop.m_itemData.m_shared.m_attack.m_attackType;
                }
            }
            return null;
        }

        private static string OriginJoint(GameObject prefab)
        {
            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            Attack? attack = drop != null ? drop.m_itemData.m_shared.m_attack : null;
            return attack?.m_attackOriginJoint ?? "";
        }

        private static IEnumerable<GameObject> All(Humanoid creature)
        {
            List<GameObject> all = new List<GameObject>();
            Add(all, creature.m_defaultItems);
            Add(all, creature.m_randomWeapon);
            Add(all, creature.m_randomShield);
            foreach (Humanoid.ItemSet set in creature.m_randomSets ?? Array.Empty<Humanoid.ItemSet>())
            {
                Add(all, set.m_items);
            }
            foreach (Humanoid.RandomItem item in creature.m_randomItems ?? Array.Empty<Humanoid.RandomItem>())
            {
                if (item.m_prefab != null)
                {
                    all.Add(item.m_prefab);
                }
            }
            return all;
        }

        private static void Add(List<GameObject> all, GameObject[]? prefabs)
        {
            foreach (GameObject prefab in prefabs ?? Array.Empty<GameObject>())
            {
                if (prefab != null)
                {
                    all.Add(prefab);
                }
            }
        }
    }
}
