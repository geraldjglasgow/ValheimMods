using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>swap</c> (stones.md section 9: Turmoil): one affix out, a different one in, the count unchanged. Never the
    /// bound affix; dormant ones may go. The replacement never repeats the removed id and comes from the removed
    /// affix's pool class. Preservation protects one affix, Culling picks the one removed, a category sigil steers the
    /// replacement.
    /// </summary>
    internal sealed class SwapVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (!AnyRemovable(job.State, null))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            string? keep = null;
            if (job.Sigil.Preserves(StoneVerb.Swap) && !Preservation.TryPick(job.State, out keep))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            if (!AnyRemovable(job.State, keep))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            return Roll(job, keep);
        }

        private static StoneResult Roll(StoneJob job, string? keep)
        {
            RemovePick pick = job.Sigil.Culls(StoneVerb.Swap) ? RemovePick.LowestTier : RemovePick.Random;
            AffixCategory? steer = job.Sigil.CategoryFor(StoneVerb.Swap);
            RollOutcome outcome = ItemRoller.Swap(job.State, pick, job.RollContext(steer), keep, out string? removed, out string? added);
            if (!outcome.Success)
            {
                return StoneResult.Refuse(outcome.Failure == RollFailure.AtMinimum ? "nothing_to_change" : "no_eligible_affix",
                    job.StoneName);
            }
            if (!VerbSupport.SteerMet(job, outcome.State!, steer))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            return StoneResult.Success(outcome.State!, "affix_swapped", job.ItemName,
                StoneNames.Affix(job.Rules, removed), StoneNames.Affix(job.Rules, added));
        }

        private static bool AnyRemovable(ItemState state, string? keep) => VerbSupport.AnyRemovable(state, keep);
    }

    /// <summary>
    /// <c>reroll_affixes</c> (stones.md section 10: Upheaval): every affix but the bound one and, with a Sigil of
    /// Preservation, the protected one is replaced, dormant ones included; the count is drawn afresh in the rarity's
    /// range with the kept ones counting toward it, and a Mythic keeps its Mythic-only slot. A category sigil steers
    /// one new affix; Culling does not steer it (SIG-5). Refused when nothing is removable or the pool cannot reach the
    /// rarity's minimum.
    /// </summary>
    internal sealed class RerollAffixesVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (!VerbSupport.AnyRemovable(job.State, null))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            string? keep = null;
            if (job.Sigil.Preserves(StoneVerb.RerollAffixes) && !Preservation.TryPick(job.State, out keep))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            if (!VerbSupport.AnyRemovable(job.State, keep))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            return Roll(job, keep);
        }

        private static StoneResult Roll(StoneJob job, string? keep)
        {
            AffixCategory? steer = job.Sigil.CategoryFor(StoneVerb.RerollAffixes);
            RollOutcome outcome = ItemRoller.Reroll(job.State, job.Rarity!, job.RollContext(steer), keep);
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            // A new affix may repeat an id the item had before the reroll, so "new" here means "not kept" (IMP-72).
            if (!VerbSupport.SteerMetBeyond(outcome.State!, steer, job.State.BoundId, keep))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            return StoneResult.Success(outcome.State!, "affixes_rerolled", job.ItemName);
        }
    }

    /// <summary>
    /// <c>reroll_values</c> (stones.md section 11: Perfection): every live, unbound, numeric affix whose stored tier
    /// still exists gets a new value inside that tier; ids, tiers and order never change. A value that lands on its
    /// old number is still a success. Preservation leaves the protected affix's value alone.
    /// </summary>
    internal sealed class RerollValuesVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (!AnyRerollable(job.State, null))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            string? keep = null;
            if (job.Sigil.Preserves(StoneVerb.RerollValues) && !Preservation.TryPick(job.State, out keep))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            if (!AnyRerollable(job.State, keep))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            RollOutcome outcome = ItemRoller.RerollValues(job.State, job.RollContext(null), keep);
            return outcome.Success
                ? StoneResult.Success(outcome.State!, "values_rerolled", job.ItemName)
                : StoneResult.Refuse("nothing_to_change", job.StoneName);
        }

        private static bool AnyRerollable(ItemState state, string? keep)
        {
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (RollOps.RerollRow(state, i, keep) != null)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>The Sigil of Preservation's protected affix (sigils.md section 3).</summary>
    internal static class Preservation
    {
        public static bool TryPick(ItemState state, out string? id)
        {
            int index = AffixRanking.Protected(state);
            id = index < 0 ? null : state.Affixes[index].Id;
            return id != null;
        }
    }
}
