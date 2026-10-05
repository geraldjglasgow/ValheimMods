using EliteCrafting.Rolling;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>reroll</c> (the Recasting Rune): between one and all of the item's affixes, dormant ones included, are rolled
    /// again in place (<see cref="ItemRoller.Recast"/>) at the item's own tiers and the rune's floor; the rest stay, so
    /// the item never loses one (user decision 2026-10-04). The rarity stays. A Magic item with no affix at all is
    /// rolled fresh at its rarity instead. Not confirm-gated by default: it is the rune a player uses again and again.
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
            RollOutcome outcome = job.State.AffixCount == 0
                ? ItemRoller.RollFresh(job.State, job.Rarity, job.RollContext())
                : ItemRoller.Recast(job.State, job.RollContext());
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            return StoneResult.Success(outcome.State!, "rerolled", job.ItemName);
        }
    }
}
