using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>promote</c> (stones.md section 7: Awakening ... Apotheosis): one rarity up, every affix kept, adds to the new
    /// minimum and at least <c>rolling.promote_adds_at_least</c>, capped at the new maximum; into a rarity with
    /// <c>mythic_affixes</c> the special affix comes first (regular pool until the Mythic-only pool exists). Awakening
    /// on a Common therefore adds one affix, per rarity.md section 3. A pool that cannot supply them refuses.
    /// </summary>
    internal sealed class PromoteVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            RarityDef? next = job.Rules.Economy.Next(job.Rarity!);
            if (next == null)
            {
                // An owner listed the top rarity in a promote stone's applies_to: there is nowhere to go.
                return StoneResult.Refuse("wrong_rarity", job.StoneName, StoneNames.Rarity(job.Rarity!));
            }
            AffixCategory? steer = job.Sigil.CategoryFor(StoneVerb.Promote);
            RollOutcome outcome = ItemRoller.Promote(job.State, next, job.RollContext(steer));
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            if (!VerbSupport.SteerMet(job, outcome.State!, steer))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            return StoneResult.Success(outcome.State!, "promoted", job.ItemName, StoneNames.Rarity(next));
        }
    }

    /// <summary>
    /// <c>add</c> (stones.md section 8: Growth): one affix, refused at the rarity's maximum (dormant affixes count).
    /// </summary>
    internal sealed class AddVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.State.AffixCount >= job.Rarity!.MaxAffixes)
            {
                return StoneResult.Refuse("affixes_full");
            }
            AffixCategory? steer = job.Sigil.CategoryFor(StoneVerb.Add);
            RollOutcome outcome = ItemRoller.AddAffixes(job.State, 1, job.RollContext(steer));
            if (!outcome.Success)
            {
                return StoneResult.Refuse("no_eligible_affix");
            }
            if (!VerbSupport.SteerMet(job, outcome.State!, steer))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            string added = VerbSupport.LastAffixId(outcome.State!);
            return StoneResult.Success(outcome.State!, "affix_added", job.ItemName, StoneNames.Affix(job.Rules, added));
        }
    }

    /// <summary>Checks shared by the verbs.</summary>
    internal static class VerbSupport
    {
        /// <summary>
        /// A category steer was honoured: some affix the stone added is of the category. The roller applies the filter
        /// to the first draw that has a candidate in the category, so no such affix means no draw had one - the sigil
        /// finds nothing to steer and the stone is refused rather than spending it (sigils.md section 3).
        /// </summary>
        public static bool SteerMet(StoneJob job, ItemState after, AffixCategory? category)
        {
            if (category == null)
            {
                return true;
            }
            for (int i = 0; i < after.AffixCount; i++)
            {
                AffixDef? def = after.DefinitionAt(i);
                if (def != null && def.Category == category && !job.State.HasAffix(after.Affixes[i].Id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A category steer was honoured by a roll that replaced everything but <paramref name="keptA"/> and
        /// <paramref name="keptB"/> (Upheaval): some affix other than those is of the category.
        /// </summary>
        public static bool SteerMetBeyond(ItemState after, AffixCategory? category, string? keptA, string? keptB)
        {
            if (category == null)
            {
                return true;
            }
            for (int i = 0; i < after.AffixCount; i++)
            {
                string id = after.Affixes[i].Id;
                if (after.DefinitionAt(i)?.Category == category && id != keptA && id != keptB)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Some affix a removal may take: every one but the bound one and <paramref name="keep"/>.</summary>
        public static bool AnyRemovable(ItemState state, string? keep)
        {
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (AffixRanking.Removable(state, i, keep))
                {
                    return true;
                }
            }
            return false;
        }

        public static string LastAffixId(ItemState state) =>
            state.AffixCount == 0 ? "" : state.Affixes[state.AffixCount - 1].Id;
    }
}
