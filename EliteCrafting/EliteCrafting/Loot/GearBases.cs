using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Loot
{
    /// <summary>One item type that can drop pre-rolled: its prefab, class, item level, pool tier and affix capacity.</summary>
    public sealed class GearBase
    {
        internal GearBase(GameObject prefab, ItemDrop template, string name, ClassInfo info, int level, int poolTier, GearCapacity capacity)
        {
            Prefab = prefab;
            Template = template;
            Name = name;
            Class = info;
            Level = level;
            PoolTier = poolTier;
            Capacity = capacity;
        }

        public GameObject Prefab { get; }
        public ItemDrop Template { get; }
        public string Name { get; }

        /// <summary>The base's item class (classes-and-tiers.md section 1).</summary>
        public ClassInfo Class { get; }

        /// <summary>The base's own item level (1-8): the level its affixes roll under, never the biome's.</summary>
        public int Level { get; }

        /// <summary>The tier the pool files it under: the item level, or its <c>gear.include</c> tier.</summary>
        public int PoolTier { get; }

        /// <summary>How many affixes a fresh roll can give it, per kind (<see cref="GearCapacity"/>).</summary>
        internal GearCapacity Capacity { get; }

        /// <summary>The most affixes a fresh roll at this rarity can give it.</summary>
        public int CapacityFor(RarityDef rarity) => Capacity.For(rarity);
    }

    /// <summary>
    /// Collects the drop-eligible magic bases from the object database (drops.md section 8, classes-and-tiers.md section
    /// 10): magic bases only (a class with <c>rolls: true</c>, never stackable, never a rune), not in <c>gear.exclude</c>,
    /// and - unless listed in <c>gear.include</c> - with a recipe (when <c>gear.require_recipe</c>), no DLC and no quest
    /// flag. A base whose pool cannot fill the lowest magic rarity's minimum is left out with one load warning.
    /// </summary>
    internal static class GearBases
    {
        public static List<GearBase> Collect(ObjectDB db, RuleSet rules)
        {
            List<GearBase> bases = new List<GearBase>();
            List<string> thin = new List<string>();
            GearDropRules gear = rules.Economy.Drops.Gear;
            HashSet<string> exclude = new HashSet<string>(gear.Exclude, System.StringComparer.Ordinal);
            RarityDef? lowest = LowestMagic(rules.Economy);
            int minimum = lowest == null ? 1 : System.Math.Max(1, lowest.MinAffixes);
            foreach (GameObject prefab in db.m_items)
            {
                GearBase? found = prefab == null ? null : TryBase(prefab, gear, exclude, rules);
                if (found != null && lowest != null && found.CapacityFor(lowest) < minimum)
                {
                    thin.Add(found.Name);
                }
                else if (found != null)
                {
                    bases.Add(found);
                }
            }
            WarnThin(thin, minimum);
            return bases;
        }

        private static void WarnThin(List<string> thin, int minimum)
        {
            if (thin.Count > 0)
            {
                Log.Warn($"gear drops: {thin.Count} bases cannot fill {minimum} inscriptions at their item level and never drop: {string.Join(", ", thin)}");
            }
        }

        private static GearBase? TryBase(GameObject prefab, GearDropRules gear, HashSet<string> exclude, RuleSet rules)
        {
            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null || !ItemClasses.IsMagicBase(drop.m_itemData))
            {
                return null;
            }
            string name = prefab.name;
            bool included = gear.Include.TryGetValue(name, out int includeTier);
            if (exclude.Contains(name) || (!included && !Eligible(drop.m_itemData, name, gear)))
            {
                return null;
            }
            int level = ItemTier.Of(name);
            ClassInfo info = ItemClasses.Classify(drop.m_itemData);
            int poolTier = included ? BiomeTiers.Clamp(includeTier) : level;
            return new GearBase(prefab, drop, name, info, level, poolTier, GearCapacity.Of(info, level, rules));
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

        private static RarityDef? LowestMagic(EconomyRules economy)
        {
            for (int i = 0; i < economy.Rarities.Count; i++)
            {
                if (!economy.Rarities[i].IsBase)
                {
                    return economy.Rarities[i];
                }
            }
            return null;
        }
    }
}
