using EliteCrafting.Rolling;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>reroll</c> (the Recasting Rune): every affix goes, dormant ones included, and the item is rolled fresh at its
    /// own rarity: the count drawn again in the rarity's range (1-2 on Magic), each affix at the item's own tiers and
    /// the rune's floor. The rarity stays. Not confirm-gated by default: it is the rune a player uses again and again.
    /// Never on the base rarity (an owner's <c>applies_to</c>): a Normal item holds no affixes, so it refuses.
    /// </summary>
    internal sealed class RerollVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.Rarity!.IsBase)
            {
                return StoneResult.Refuse("wrong_rarity", job.StoneName, StoneNames.Rarity(job.Rarity));
            }
            RollOutcome outcome = ItemRoller.RollFresh(job.State, job.Rarity, job.RollContext());
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            return StoneResult.Success(outcome.State!, "rerolled", job.ItemName);
        }
    }
}
