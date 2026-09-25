using System;
using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The economy's <c>salvage:</c> section (salvage.md section 7): whether grinding asks first, where it may happen,
    /// what each rarity grinds into and what the five shards fuse into. The on/off switch is the <c>.cfg</c>
    /// (<c>8 - Salvage</c>), not here. Immutable.
    /// </summary>
    public sealed class SalvageRules
    {
        public bool Confirm { get; internal set; } = true;

        /// <summary>Station prefab names; empty = grind anywhere.</summary>
        public IReadOnlyList<string> Stations { get; internal set; } = Array.Empty<string>();

        /// <summary>rarity id → yield rows. A rarity absent here grinds into nothing (<c>salvage_no_yield</c>).</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<SalvageYield>> Yields { get; internal set; } =
            new Dictionary<string, IReadOnlyList<SalvageYield>>();

        public IReadOnlyList<FragmentDef> Fragments { get; internal set; } = Array.Empty<FragmentDef>();
        public IReadOnlyDictionary<string, FragmentDef> FragmentById { get; internal set; } = new Dictionary<string, FragmentDef>();
        public IReadOnlyDictionary<string, FragmentDef> FragmentByPrefab { get; internal set; } = new Dictionary<string, FragmentDef>();

        public FragmentDef? Fragment(string? id) => id != null && FragmentById.TryGetValue(id, out FragmentDef f) ? f : null;

        public FragmentDef? FragmentForPrefab(string? prefab) =>
            prefab != null && FragmentByPrefab.TryGetValue(prefab, out FragmentDef f) ? f : null;

        /// <summary>The yield rows of a rarity; empty when it has none.</summary>
        public IReadOnlyList<SalvageYield> YieldOf(string rarityId) =>
            Yields.TryGetValue(rarityId, out IReadOnlyList<SalvageYield> rows) ? rows : Array.Empty<SalvageYield>();
    }

    /// <summary>One yield row: <c>amount</c> shards of <c>fragment</c>, paid with <c>chance</c> percent.</summary>
    public sealed class SalvageYield
    {
        public string Fragment { get; internal set; } = "";
        public int Amount { get; internal set; } = 1;

        /// <summary>Percent, 0-100.</summary>
        public float Chance { get; internal set; } = 100f;
    }

    /// <summary>One shard entry of <c>salvage.fragments</c> (salvage.md section 5), bound to its built-in prefab.</summary>
    public sealed class FragmentDef
    {
        public string Id { get; internal set; } = "";

        /// <summary><c>ECF_</c> + PascalCase id (<c>ECF_ShardAscension</c>).</summary>
        public string Prefab { get; internal set; } = "";

        /// <summary>The stone id the shards fuse into.</summary>
        public string Stone { get; internal set; } = "";

        public int Fuse { get; internal set; } = 5;

        /// <summary>A <c>$key</c> (default <c>$ecf_fragment_&lt;id&gt;</c>) or literal text.</summary>
        public string Name { get; internal set; } = "";

        public string Description { get; internal set; } = "";
        public int Stack { get; internal set; } = 50;
        public float ItemWeight { get; internal set; } = 0.1f;

        /// <summary><c>#RRGGBB</c> override of the target stone's tint, or null.</summary>
        public string? Tint { get; internal set; }
    }
}
