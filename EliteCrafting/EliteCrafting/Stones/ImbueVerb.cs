using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>imbue</c> (essences.md section 4): an essence rerolls the item like Upheaval - the bound affix and a
    /// Preservation-protected one stay, everything else goes - and one new affix always comes from the stone's family,
    /// at the stone's <c>tier_floor</c> (Greater: the item's ceiling). Refused before rolling when the family has
    /// nothing for this kind of item (<c>essence_wrong_item</c>) or there is no room beside the kept affixes
    /// (<c>affixes_full</c>); in the dry run when no member is eligible (<c>essence_no_match</c>). Never
    /// <c>nothing_to_change</c>. Preservation steers; War, Warding, Fortune and Culling stay pending (ESS-7).
    /// </summary>
    internal sealed class ImbueVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            EssenceFamilyDef? family = job.Rules.Economy.Family(job.Def!.Family);
            if (family == null)
            {
                return StoneResult.Refuse("stone_disabled", job.StoneName);
            }
            if (!EssenceMembers.AnyFor(job.Rules, family, job.Slot))
            {
                return StoneResult.Refuse("essence_wrong_item", job.StoneName);
            }
            string? keep = null;
            if (job.Sigil.Preserves(StoneVerb.Imbue) && !Preservation.TryPick(job.State, out keep))
            {
                return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
            }
            if (KeptCount(job.State, keep) + 1 > job.Rarity!.MaxAffixes)
            {
                return StoneResult.Refuse("affixes_full");
            }
            return Roll(job, family, keep);
        }

        private static StoneResult Roll(StoneJob job, EssenceFamilyDef family, string? keep)
        {
            // The stone's floor binds the guaranteed affix only (ESS-4); the other affixes roll in the plain window.
            RollContext context = job.RollContext(null);
            context.TierFloor = 0;
            ImbueRequest request = new ImbueRequest(family.Affixes, job.Def!.TierFloor, keep);
            RollOutcome outcome = ItemRoller.Imbue(job.State, job.Rarity!, context, request, out string? guaranteed);
            if (!outcome.Success)
            {
                return outcome.Failure == RollFailure.NoFamilyMatch
                    ? StoneResult.Refuse("essence_no_match", job.StoneName)
                    : StoneResult.Refuse("no_eligible_affix");
            }
            return StoneResult.Success(outcome.State!, "essence_applied", job.ItemName, StoneNames.Affix(job.Rules, guaranteed));
        }

        // The kept set: the bound affix and the protected one (sigils.md section 3).
        private static int KeptCount(ItemState state, string? keep)
        {
            int kept = 0;
            for (int i = 0; i < state.AffixCount; i++)
            {
                kept += state.IsBoundAt(i) || state.Affixes[i].Id == keep ? 1 : 0;
            }
            return kept;
        }
    }

    /// <summary>The step-11 precondition of an essence: some member of its family could ever roll on this kind of item.</summary>
    internal static class EssenceMembers
    {
        /// <summary>
        /// A member that is defined, enabled, not Mythic-only, lists the item's slot and passes its <c>requires</c>
        /// (tiers and exclusion are the roll's business). A handful of dictionary lookups; runs on a click, not per frame.
        /// </summary>
        public static bool AnyFor(RuleSet rules, EssenceFamilyDef family, SlotInfo slot)
        {
            foreach (string id in family.Affixes)
            {
                AffixDef? def = rules.Affixes.Get(id);
                if (def != null && def.Enabled && !def.MythicOnly && def.RollsOn(slot.Slot) && ItemSlots.Satisfies(slot, def.Requires))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
