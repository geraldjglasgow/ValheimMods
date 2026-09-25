using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>The <c>item_tiers:</c> section (item-tier.md). Read through <c>Items.ItemTier</c>, never directly per roll.</summary>
    public sealed class ItemTierMaps
    {
        public IReadOnlyDictionary<string, int> Items { get; internal set; } = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, int> Materials { get; internal set; } = new Dictionary<string, int>();
        public int MaterialDepth { get; internal set; } = 3;
        public IReadOnlyDictionary<string, int> Stations { get; internal set; } = new Dictionary<string, int>();

        /// <summary>station → (minimum station level → tier).</summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<int, int>> StationLevels { get; internal set; } =
            new Dictionary<string, IReadOnlyDictionary<int, int>>();

        public int FallbackTier { get; internal set; } = 1;
    }
}
