using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>corrupt</c> (stones.md section 14: the Serpent Stone, confirm-gated): one outcome drawn by weight from the
    /// stone's <c>outcomes</c> table, carried out on the copy, then the item is sealed (<c>ecf_sealed = serpent</c>).
    /// An outcome that cannot be carried out falls back to <c>seal_only</c>, so once the checks pass it always
    /// succeeds. Refused while a live sigil is pending (SIG-2, <c>sigil_would_strand</c>); a dormant one never steers
    /// again anyway and is sealed in with the item (IMP-65). The refine bonus is untouched by every outcome.
    /// <para>
    /// The outcome is drawn in the dry run and that dry run is what commits; with the Dialog confirm mode the Yes
    /// re-runs the pipeline and so draws afresh (the player never saw the first draw). Local client only; the
    /// transient corruption effect for nearby peers is Phase 3 - this phase shows the message only.
    /// </para>
    /// </summary>
    internal sealed class CorruptVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.Sigil.IsLive)
            {
                return StoneResult.Refuse("sigil_would_strand");
            }
            RollContext context = job.RollContext(null);
            CorruptOutcome outcome = Draw(job.Def!.Outcomes, context);
            StoneResult? result = SerpentOutcomes.Carry(job, outcome, context);
            return result ?? SerpentOutcomes.SealOnly(job);
        }

        // Weights are relative; a table with nothing above 0 is disabled at load, and falls back to seal_only here.
        private static CorruptOutcome Draw(IReadOnlyList<CorruptWeight> table, RollContext context)
        {
            List<float> weights = new List<float>(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                weights.Add(table[i].Weight);
            }
            int index = RollMath.PickWeighted(weights, context.Random);
            return index < 0 ? CorruptOutcome.SealOnly : table[index].Outcome;
        }
    }

    /// <summary>
    /// The five Serpent outcomes (stones.md section 14 table). Each returns the sealed success, or null when it cannot
    /// be carried out and the caller falls back to <see cref="SealOnly"/>.
    /// </summary>
    internal static class SerpentOutcomes
    {
        public static StoneResult? Carry(StoneJob job, CorruptOutcome outcome, RollContext context)
        {
            switch (outcome)
            {
                case CorruptOutcome.AddAffix:
                    return AddAffix(job, context, job.Def!.Overflow);
                case CorruptOutcome.ChaoticReroll:
                    return Chaotic(job, context);
                case CorruptOutcome.Promote:
                    return Promote(job, context);
                case CorruptOutcome.Demote:
                    return Demote(job, context);
                default:
                    return null;
            }
        }

        public static StoneResult SealOnly(StoneJob job) =>
            StoneResult.Success(Sealed(job.State), "corrupt_seal", job.ItemName);

        // One affix by the normal roll; it may take the item past its rarity's maximum by `overflow` (default 1). Never
        // on the base rarity (an owner's applies_to): a Common holds no affixes, so that falls back to seal_only.
        private static StoneResult? AddAffix(StoneJob job, RollContext context, int overflow)
        {
            if (job.Rarity!.IsBase || job.State.AffixCount >= job.Rarity.MaxAffixes + overflow)
            {
                return null;
            }
            return Added(job, ItemRoller.AddAffixes(job.State, 1, context), "corrupt_add");
        }

        // Every affix out (the binding too), rolled again at any tier the affix defines; nothing rolled = seal only.
        private static StoneResult? Chaotic(StoneJob job, RollContext context)
        {
            RollOutcome rolled = ItemRoller.RollChaotic(job.State, job.Rarity!, context);
            return rolled.Success ? StoneResult.Success(Sealed(rolled.State!), "corrupt_chaos", job.ItemName) : null;
        }

        // One rarity up; on the top rarity one regular affix beyond the cap instead (a Mythic's 7th), reported as a gain (IMP-70).
        private static StoneResult? Promote(StoneJob job, RollContext context)
        {
            RarityDef? next = job.Rules.Economy.Next(job.Rarity!);
            if (next == null)
            {
                return Added(job, ItemRoller.AddAffixes(job.State, 1, context), "corrupt_add");
            }
            RollOutcome rolled = ItemRoller.Promote(job.State, next, context);
            return rolled.Success
                ? StoneResult.Success(Sealed(rolled.State!), "corrupt_promote", job.ItemName, StoneNames.Rarity(next))
                : null;
        }

        // One rarity down and an affix lost (the bound one eligible); nowhere to go (a custom applies_to) = seal only (IMP-70).
        private static StoneResult? Demote(StoneJob job, RollContext context)
        {
            RarityDef? previous = job.Rules.Economy.Previous(job.Rarity!);
            if (previous == null)
            {
                return null;
            }
            RollOutcome rolled = ItemRoller.Demote(job.State, previous, context);
            return StoneResult.Success(Sealed(rolled.State!), "corrupt_demote", job.ItemName, StoneNames.Rarity(previous));
        }

        private static StoneResult? Added(StoneJob job, RollOutcome rolled, string feedbackId)
        {
            if (!rolled.Success)
            {
                return null;
            }
            string added = VerbSupport.LastAffixId(rolled.State!);
            return StoneResult.Success(Sealed(rolled.State!), feedbackId, job.ItemName, StoneNames.Affix(job.Rules, added));
        }

        private static ItemState Sealed(ItemState state) => state.ToBuilder().Seal(ItemKeys.SealedSerpent).Build();
    }
}
