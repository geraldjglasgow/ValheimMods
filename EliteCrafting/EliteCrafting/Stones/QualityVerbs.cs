using EliteCrafting.Affixes;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>quality</c> (quality.md: Honing and Tempering): the item's refine bonus (<c>ecf_refine</c>, percent points)
    /// rises by the stone's <c>step</c>, the last step clipped to its <c>cap</c>; at or above the cap it is refused
    /// (<c>quality_capped</c>). A value above a lowered cap is kept, never cut. Independent of rarity and affixes; no
    /// sigil steers it. The Effects getters apply the bonus by the item's slot - damage on weapons, armor on armor,
    /// block power on shields - which is exactly the set <see cref="QualityTargets"/> admits.
    /// </summary>
    internal sealed class QualityVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            float current = job.State.Refine;
            int cap = job.Def!.Cap;
            if (current >= cap)
            {
                return StoneResult.Refuse("quality_capped", job.StoneName);
            }
            float next = System.Math.Min(current + job.Def.Step, cap);
            ItemState refined = job.State.ToBuilder().SetRefine(next).Build();
            string feedback = QualityTargets.IsHoned(job.Slot.Slot) ? "honed" : "tempered";
            return StoneResult.Success(refined, feedback, job.ItemName, Numbers.Format(next));
        }
    }

    /// <summary>
    /// The quality stones' base precondition (pipeline step 5, quality.md section 5): the item has the number the
    /// bonus multiplies. Weapons need damage of their own (a summoning staff has none), armor pieces armor, shields
    /// block power, each counting the per-upgrade-level growth. Tools and utility items have no refine getter in
    /// Effects, so they are refused even when an owner lists their slot (IMP-69): a stone that visibly does nothing
    /// must not be spendable. Reads the item type's shared data only.
    /// </summary>
    internal static class QualityTargets
    {
        public static bool CanImprove(ItemDrop.ItemData item, ItemSlot slot)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            switch (slot)
            {
                case ItemSlot.MeleeWeapon: case ItemSlot.RangedWeapon: case ItemSlot.MagicWeapon:
                    return shared.m_damages.GetTotalDamage() > 0f || shared.m_damagesPerLevel.GetTotalDamage() > 0f;
                case ItemSlot.Head: case ItemSlot.Chest: case ItemSlot.Legs: case ItemSlot.Cape:
                    return shared.m_armor > 0f || shared.m_armorPerLevel > 0f;
                case ItemSlot.Shield:
                    return shared.m_blockPower > 0f || shared.m_blockPowerPerLevel > 0f;
                default:
                    return false;
            }
        }

        /// <summary>"Honed" on weapons, "Tempered" on armor and shields (QLT-6; Display's tooltip line agrees).</summary>
        public static bool IsHoned(ItemSlot slot) =>
            slot == ItemSlot.MeleeWeapon || slot == ItemSlot.RangedWeapon || slot == ItemSlot.MagicWeapon;
    }

    /// <summary>
    /// <c>sigil</c> (sigils.md section 1): the item gains the pending sigil (<c>ecf_sigil</c> = the sigil's stone id).
    /// One per item: refused while a live sigil waits (<c>sigil_pending</c>); a dormant one (id gone, disabled, not a
    /// sigil) is replaced without refusal (SIG-8). Any rarity, Common included; nothing about the item's effects
    /// changes. Sealed items were already refused by the pipeline (STN-1).
    /// </summary>
    internal sealed class SigilVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.State.HasSigil && job.Sigil.IsLive)
            {
                return StoneResult.Refuse("sigil_pending");
            }
            ItemState pending = job.State.ToBuilder().SetSigil(job.Def!.Id).Build();
            return StoneResult.Success(pending, "sigil_set", job.ItemName, job.StoneName);
        }
    }
}
