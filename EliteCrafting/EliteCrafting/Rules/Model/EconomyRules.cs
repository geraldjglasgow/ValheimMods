using System;
using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The economy family (<c>EliteCrafting_economy*.yml</c>) after merging and validation, with lookups and the
    /// per-tier drop draw tables precomputed at load. Immutable.
    /// </summary>
    public sealed class EconomyRules
    {
        public static readonly EconomyRules Empty = new EconomyRules();

        /// <summary>In ladder order, lowest first; index 0 is the base rarity.</summary>
        public IReadOnlyList<RarityDef> Rarities { get; internal set; } = Array.Empty<RarityDef>();

        public RollingSettings Rolling { get; internal set; } = new RollingSettings();
        public IReadOnlyList<StoneDef> Stones { get; internal set; } = Array.Empty<StoneDef>();
        public SigilSettings Sigils { get; internal set; } = new SigilSettings();
        public ItemTierMaps ItemTiers { get; internal set; } = new ItemTierMaps();

        /// <summary>biome id (<c>meadows</c>, <c>black_forest</c>, ..., <c>ocean</c>, <c>deep_north</c>) → tier.</summary>
        public IReadOnlyDictionary<string, int> Biomes { get; internal set; } = new Dictionary<string, int>();

        public DropRules Drops { get; internal set; } = new DropRules();

        /// <summary>family id → the affixes an <c>imbue</c> stone of that family guarantees one of (essences.md 11).</summary>
        public IReadOnlyDictionary<string, EssenceFamilyDef> EssenceFamilies { get; internal set; } =
            new Dictionary<string, EssenceFamilyDef>();

        /// <summary>Grinding and fusing (salvage.md section 7).</summary>
        public SalvageRules Salvage { get; internal set; } = new SalvageRules();

        // lookups, precomputed at load
        public IReadOnlyDictionary<string, RarityDef> RarityById { get; internal set; } = new Dictionary<string, RarityDef>();
        public IReadOnlyDictionary<string, StoneDef> StoneById { get; internal set; } = new Dictionary<string, StoneDef>();
        public IReadOnlyDictionary<string, StoneDef> StoneByPrefab { get; internal set; } = new Dictionary<string, StoneDef>();

        /// <summary>The rarity with <c>mythic_affixes &gt; 0</c>, if any.</summary>
        public RarityDef? MythicRarity { get; internal set; }

        internal WeightedTable<StoneDef>[] StoneTables { get; set; } = Array.Empty<WeightedTable<StoneDef>>();
        internal WeightedTable<RarityDef>[] GearTables { get; set; } = Array.Empty<WeightedTable<RarityDef>>();
        internal WeightedTable<RarityDef>[] BossGearTables { get; set; } = Array.Empty<WeightedTable<RarityDef>>();

        public RarityDef? BaseRarity => Rarities.Count > 0 ? Rarities[0] : null;

        public RarityDef? Rarity(string? id) => id != null && RarityById.TryGetValue(id, out RarityDef r) ? r : null;

        public StoneDef? Stone(string? id) => id != null && StoneById.TryGetValue(id, out StoneDef s) ? s : null;

        public StoneDef? StoneForPrefab(string? prefab) =>
            prefab != null && StoneByPrefab.TryGetValue(prefab, out StoneDef s) ? s : null;

        public EssenceFamilyDef? Family(string? id) =>
            id != null && EssenceFamilies.TryGetValue(id, out EssenceFamilyDef f) ? f : null;

        public RarityDef? Next(RarityDef rarity) => rarity.Index + 1 < Rarities.Count ? Rarities[rarity.Index + 1] : null;

        public RarityDef? Previous(RarityDef rarity) => rarity.Index > 0 ? Rarities[rarity.Index - 1] : null;

        /// <summary>Which stone drops at a tier (1-7): enabled stones only, weights from <c>drops.stones</c>.</summary>
        public WeightedTable<StoneDef> StoneDraw(int tier) => ByTier(StoneTables, tier);

        /// <summary>The rarity of dropped gear at a tier, <c>drop_weight</c> applied; the boss table when asked.</summary>
        public WeightedTable<RarityDef> GearRarityDraw(int tier, bool boss) => ByTier(boss ? BossGearTables : GearTables, tier);

        /// <summary>The tier of a biome id, or 0 when unmapped.</summary>
        public int BiomeTier(string biomeId) => Biomes.TryGetValue(biomeId, out int tier) ? tier : 0;

        private static WeightedTable<T> ByTier<T>(WeightedTable<T>[] tables, int tier) =>
            tier >= 1 && tier <= tables.Length ? tables[tier - 1] : WeightedTable<T>.Empty;
    }
}
