using EliteCrafting.Affixes;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>strip</c> (the Cleansing Rune, confirm-gated): every affix goes, dormant ones included, and the item becomes
    /// the base rarity. Kept: the vanilla upgrade level, durability, crafter and variant (none of which live in our
    /// keys), and unreadable segments (item-data.md section 4: never destroyed, IMP-71). Sealed is permanent: the
    /// pipeline has already refused a sealed item.
    /// </summary>
    internal sealed class StripVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            ItemState stripped = job.State.ToBuilder().ClearAffixes().SetRarity(null).Build();
            return StoneResult.Success(stripped, "stripped", job.ItemName);
        }
    }
}
