using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Loot
{
    /// <summary>One item type that can drop pre-rolled: its prefab, slot, affix ceiling, pool tier and affix capacity.</summary>
    public sealed class GearBase
    {
        internal GearBase(GameObject prefab, ItemDrop template, string name, SlotInfo slot, int ceiling, int poolTier, int capacity)
        {
            Prefab = prefab;
            Template = template;
            Name = name;
            Slot = slot;
            Ceiling = ceiling;
            PoolTier = poolTier;
            Capacity = capacity;
        }

        public GameObject Prefab { get; }
        public ItemDrop Template { get; }
        public string Name { get; }
        public SlotInfo Slot { get; }

        /// <summary>The base's own tier (item-tier.md): the ceiling its affixes roll under, never the biome's.</summary>
        public int Ceiling { get; }

        /// <summary>The tier the pool files it under: the ceiling, or its <c>gear.include</c> tier.</summary>
        public int PoolTier { get; }

        /// <summary>The most affixes a fresh roll can give it (<see cref="GearCapacity"/>).</summary>
        public int Capacity { get; }
    }

    /// <summary>
    /// Collects the drop-eligible magic bases from the object database (drops.md section 8): magic bases only (never
    /// stackable, never a stone), not in <c>gear.exclude</c>, and - unless listed in <c>gear.include</c> - with a
    /// recipe (when <c>gear.require_recipe</c>), no DLC and no quest flag. A base whose pool cannot fill the lowest
    /// magic rarity's minimum is left out with one load warning.
    /// </summary>
    internal static class GearBases
    {
        public static List<GearBase> Collect(ObjectDB db, RuleSet rules)
        {
            List<GearBase> bases = new List<GearBase>();
            List<string> thin = new List<string>();
            GearDropRules gear = rules.Economy.Drops.Gear;
            HashSet<string> exclude = new HashSet<string>(gear.Exclude, System.StringComparer.Ordinal);
            int minimum = LowestMagicMinimum(rules.Economy);
            foreach (GameObject prefab in db.m_items)
            {
                GearBase? found = prefab == null ? null : TryBase(prefab, gear, exclude, rules.Affixes);
                if (found != null && found.Capacity < minimum)
                {
                    thin.Add(found.Name);
                }
                else if (found != null)
                {
                    bases.Add(found);
                }
            }
            if (thin.Count > 0)
            {
                Log.Warn($"gear drops: {thin.Count} bases cannot fill {minimum} affixes at their tier and never drop: {string.Join(", ", thin)}");
            }
            return bases;
        }

        private static GearBase? TryBase(GameObject prefab, GearDropRules gear, HashSet<string> exclude, AffixRules affixes)
        {
            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null || !ItemSlots.IsMagicBase(drop.m_itemData))
            {
                return null;
            }
            string name = prefab.name;
            bool included = gear.Include.TryGetValue(name, out int includeTier);
            if (exclude.Contains(name) || (!included && !Eligible(drop.m_itemData, name, gear)))
            {
                return null;
            }
            int ceiling = ItemTier.Of(name);
            SlotInfo slot = ItemSlots.Classify(drop.m_itemData);
            int poolTier = included ? BiomeTiers.Clamp(includeTier) : ceiling;
            return new GearBase(prefab, drop, name, slot, ceiling, poolTier, GearCapacity.Of(slot, ceiling, affixes));
        }

        private static bool Eligible(ItemDrop.ItemData item, string name, GearDropRules gear)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            if (!string.IsNullOrEmpty(shared.m_dlc) || shared.m_questItem)
            {
                return false;
            }
            return !gear.RequireRecipe || ItemTier.HasRecipe(name);
        }

        private static int LowestMagicMinimum(EconomyRules economy)
        {
            for (int i = 0; i < economy.Rarities.Count; i++)
            {
                if (!economy.Rarities[i].IsBase)
                {
                    return System.Math.Max(1, economy.Rarities[i].MinAffixes);
                }
            }
            return 1;
        }
    }
}
