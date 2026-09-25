using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// The pre-rolled gear pool per drop tier (drops.md sections 8 and 13): bases at the drop tier weigh
    /// <c>same_tier_weight</c>, bases up to <c>tiers_below</c> below it weigh <c>lower_tier_weight</c>, both times the
    /// slot weight. Rebuilt when the rules generation, the object database instance, its item count or its recipe count
    /// changes (other mods add items and recipes late), so the first kill after a change rebuilds and every other kill
    /// costs four comparisons. Main thread only.
    /// </summary>
    public static class GearPool
    {
        private static WeightedTable<GearBase>[] _tables = Array.Empty<WeightedTable<GearBase>>();
        private static IReadOnlyList<GearBase> _bases = Array.Empty<GearBase>();
        private static int _generation = -1;
        private static ObjectDB? _db;
        private static int _items = -1;
        private static int _recipes = -1;

        /// <summary>Every drop-eligible base of the current build (for the reference command).</summary>
        public static IReadOnlyList<GearBase> Bases
        {
            get
            {
                EnsureFresh();
                return _bases;
            }
        }

        /// <summary>The weighted base draw for a drop tier; empty before the object database exists.</summary>
        public static WeightedTable<GearBase> ForTier(int tier)
        {
            EnsureFresh();
            return tier >= 1 && tier <= _tables.Length ? _tables[tier - 1] : WeightedTable<GearBase>.Empty;
        }

        internal static void Invalidate() => _generation = -1;

        private static void EnsureFresh()
        {
            ObjectDB? db = ObjectDB.instance;
            RuleSet rules = ActiveRules.Current;
            int items = db?.m_items?.Count ?? -1;
            int recipes = db?.m_recipes?.Count ?? -1;
            if (_generation == rules.Generation && ReferenceEquals(db, _db) && items == _items && recipes == _recipes)
            {
                return;
            }
            _generation = rules.Generation;
            _db = db;
            _items = items;
            _recipes = recipes;
            Build(db, rules);
        }

        private static void Build(ObjectDB? db, RuleSet rules)
        {
            _bases = db?.m_items == null ? Array.Empty<GearBase>() : (IReadOnlyList<GearBase>)GearBases.Collect(db, rules);
            _tables = new WeightedTable<GearBase>[7];
            for (int tier = 1; tier <= 7; tier++)
            {
                _tables[tier - 1] = WeightedTable<GearBase>.Build(Weights(_bases, tier, rules.Economy.Drops.Gear));
            }
            if (db != null)
            {
                Log.Info($"gear drop pool: {_bases.Count} bases; per tier {Sizes()}");
            }
        }

        private static IEnumerable<KeyValuePair<GearBase, float>> Weights(IReadOnlyList<GearBase> bases, int tier, GearDropRules gear)
        {
            foreach (GearBase b in bases)
            {
                int below = tier - b.PoolTier;
                float weight = below == 0 ? gear.SameTierWeight : below > 0 && below <= gear.TiersBelow ? gear.LowerTierWeight : 0f;
                yield return new KeyValuePair<GearBase, float>(b, weight * gear.SlotWeight(b.Slot.Slot));
            }
        }

        private static string Sizes()
        {
            string[] parts = new string[_tables.Length];
            for (int i = 0; i < _tables.Length; i++)
            {
                parts[i] = $"T{i + 1} {_tables[i].Count}";
            }
            return string.Join(", ", parts);
        }
    }
}
