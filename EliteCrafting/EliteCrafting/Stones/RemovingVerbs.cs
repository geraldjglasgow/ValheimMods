using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>remove</c> (stones.md section 12: Severing): one affix out, never below the rarity's minimum (dormant affixes
    /// count). Never the bound one; dormant ones may go. A Sigil of Culling picks the culled affix (dormant first,
    /// then the lowest tier and worst roll); Preservation and the category sigils do not steer it and stay pending.
    /// </summary>
    internal sealed class RemoveVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.State.AffixCount <= job.Rarity!.MinAffixes)
            {
                return StoneResult.Refuse("affixes_minimum");
            }
            if (!VerbSupport.AnyRemovable(job.State, null))
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            RemovePick pick = job.Sigil.Culls(StoneVerb.Remove) ? RemovePick.LowestTier : RemovePick.Random;
            RollOutcome outcome = ItemRoller.RemoveOne(job.State, pick, job.RollContext(null), out string? removed);
            if (!outcome.Success)
            {
                return StoneResult.Refuse("nothing_to_change", job.StoneName);
            }
            return StoneResult.Success(outcome.State!, "affix_removed", job.ItemName, StoneNames.Affix(job.Rules, removed));
        }
    }

    /// <summary>
    /// <c>strip</c> (stones.md section 13: Unmaking, confirm-gated): every affix goes, bound and dormant included, the
    /// binding with them, and the item becomes the base rarity. Kept: the refine bonus (quality.md section 4), a
    /// pending sigil (SIG-3), the vanilla upgrade level, durability, crafter and variant (none of which live in our
    /// keys), and unreadable segments (item-data.md section 4: never destroyed, IMP-71). Sealed is permanent: the pipeline has
    /// already refused a sealed item.
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
