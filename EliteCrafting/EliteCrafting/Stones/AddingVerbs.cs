using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>promote</c> (the Awakening and Ascension Runes): one rarity up, every affix kept, adds to the new minimum and
    /// at least <c>rolling.promote_adds_at_least</c>, capped at the new maximum. Awakening on a Normal item therefore
    /// adds one affix, Ascension on a two-inscription Magic item one more (two from a one-inscription item, to reach
    /// Rare's minimum of three), per rarity.md section 3. A pool that cannot supply them refuses.
    /// </summary>
    internal sealed class PromoteVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            RarityDef? next = job.Rules.Economy.Next(job.Rarity!);
            if (next == null)
            {
                // An owner listed the top rarity in a promote rune's applies_to: there is nowhere to go.
                return StoneResult.Refuse("wrong_rarity", job.StoneName, StoneNames.Rarity(job.Rarity!));
            }
            RollOutcome outcome = ItemRoller.Promote(job.State, next, job.RollContext());
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            return StoneResult.Success(outcome.State!, "promoted", job.ItemName, StoneNames.Rarity(next));
        }
    }
}
