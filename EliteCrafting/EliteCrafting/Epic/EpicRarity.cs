using EliteCrafting.Rules;

namespace EliteCrafting.Epic
{
    /// <summary>
    /// Epic Loot's rarities on our ladder, rung by rung: an item without Epic Loot magic stands on the base rarity
    /// (Normal), Epic Loot's Magic on the next (Magic), its Rare on the one after (Rare). Epic Loot's Epic and above have
    /// no rung on the default ladder, so the runes refuse them. The runes' <c>applies_to</c> and costs then read as they
    /// do for our own items.
    /// </summary>
    internal static class EpicRarity
    {
        public static RarityDef? Of(EpicItem? item, EconomyRules economy)
        {
            int index = item == null ? 0 : item.Rarity + 1;
            return index >= 0 && index < economy.Rarities.Count ? economy.Rarities[index] : null;
        }

        /// <summary>Epic Loot's rarity index for a rung above the base (Magic 0, Rare 1).</summary>
        public static int EpicIndex(RarityDef rarity) => rarity.Index - 1;
    }
}
