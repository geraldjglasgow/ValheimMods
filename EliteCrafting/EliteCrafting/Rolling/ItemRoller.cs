using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// CONTRACT - implemented by the Stones area. The one affix-drawing procedure of the mod (rarity.md section 4):
    /// every stone that adds affixes, the Stone of Chance, pre-rolled drops and <c>ecraft roll</c> call these. Every
    /// method is pure: it takes a state, returns a new state or a failure, and never writes the item; the caller
    /// commits with <see cref="ItemState.Write"/>. Rolls run on the peer that owns the item, under the synced rules.
    /// <para>
    /// Shared rules for every method: candidates are enabled affixes with weight above 0 whose slots contain the
    /// item's slot, that pass <c>requires</c> (<c>Items.ItemSlots.Satisfies</c>), are not on the item (dormant copies
    /// included), share no exclusion group with an affix on the item, pass the category steer, are not
    /// <see cref="RollContext.ExcludeId"/>, and have an eligible tier. Tier window:
    /// <c>low = max(ceiling - rolling.tier_window + 1, tier_floor, 1)</c>, <c>high = ceiling</c>; an affix with no
    /// tier in the window but tiers below it is eligible at its highest tier at or below the ceiling (if at least the
    /// floor). Draw: affix by <c>weight</c>, then tier by tier <c>weight</c>, then value uniformly in [min, max]
    /// rounded to the tier's decimals (flags store 1). Chaotic rolls use every tier the affix defines, uniformly.
    /// </para>
    /// </summary>
    public static class ItemRoller
    {
        /// <summary>
        /// Rolls an item fresh at <paramref name="rarity"/> (pre-rolled drops, Chance, Upheaval, <c>ecraft roll</c>):
        /// every affix except the bound one is replaced; the count is drawn in [min, max] of the rarity (uniform, or
        /// <c>rolling.count_weights</c>), kept affixes counting toward it; a rarity with <c>mythic_affixes</c> draws that
        /// many from the Mythic-only pool first (regular pool as fallback). Sets the rarity. Fails with
        /// <see cref="RollFailure.NoEligibleAffix"/> when the pool cannot reach the rarity's minimum (drops then fall back
        /// to a lower rarity themselves).
        /// </summary>
        public static RollOutcome RollFresh(ItemState current, RarityDef rarity, RollContext context)
        {
            return RollLog.Report("fresh " + rarity.Id, current, RollOps.RollFresh(current, rarity, context), context);
        }

        /// <summary>
        /// Adds exactly <paramref name="count"/> affixes at the end of the list (Growth, promotion, Serpent's add),
        /// steered by <see cref="RollContext.Category"/> when set. Does not change the rarity and does not check the
        /// rarity's maximum (the caller decides whether the item has room). All or nothing: fails with
        /// <see cref="RollFailure.NoEligibleAffix"/> when fewer candidates exist than needed.
        /// </summary>
        public static RollOutcome AddAffixes(ItemState current, int count, RollContext context)
        {
            return RollLog.Report("add " + count, current, RollOps.AddAffixes(current, count, context), context);
        }

        /// <summary>
        /// Rerolls the values of the item's affixes within each affix's stored tier (Perfection): active, unbound affixes
        /// only; dormant affixes are never rerolled; an affix whose stored tier no longer exists in its definition
        /// is skipped (stones.md section 11: treated like dormant for this stone). Order and ids are unchanged. Fails with
        /// <see cref="RollFailure.NothingToReroll"/> when no affix qualifies.
        /// </summary>
        public static RollOutcome RerollValues(ItemState current, RollContext context)
        {
            return RerollValues(current, context, null);
        }

        /// <summary>
        /// Removes one affix (Severing, Turmoil's first half, demotion): never the bound one; picked per
        /// <paramref name="pick"/>. The removed id is reported through <paramref name="removedId"/> so Turmoil can set
        /// <see cref="RollContext.ExcludeId"/> for its add. Does not check the rarity's minimum (the caller does).
        /// Fails with <see cref="RollFailure.AtMinimum"/> when nothing is removable.
        /// </summary>
        public static RollOutcome RemoveOne(ItemState current, RemovePick pick, RollContext context, out string? removedId)
        {
            return RemoveOne(current, pick, context, null, out removedId);
        }

        // ---- Phase 1 additions by the Stones area (additive; the four methods above are the contract)

        /// <summary>
        /// Promotes one rarity up to <paramref name="to"/> (rarity.md section 3): every affix kept, bound and dormant
        /// included; adds <c>max(to.min - count, rolling.promote_adds_at_least)</c> capped at <c>to.max</c>, drawing a
        /// rarity's <c>mythic_affixes</c> from the Mythic-only pool first (regular pool as fallback). All or nothing:
        /// <see cref="RollFailure.NoEligibleAffix"/> when the pool cannot supply them. Sets the rarity.
        /// </summary>
        public static RollOutcome Promote(ItemState current, RarityDef to, RollContext context)
        {
            return RollLog.Report("promote " + to.Id, current, RollOps.Promote(current, to, context), context);
        }

        /// <summary><see cref="RerollValues(ItemState, RollContext)"/> leaving <paramref name="keepId"/> untouched (Preservation).</summary>
        public static RollOutcome RerollValues(ItemState current, RollContext context, string? keepId)
        {
            return RollLog.Report("reroll values", current, RollOps.RerollValues(current, context, keepId), context);
        }

        /// <summary><see cref="RemoveOne(ItemState, RemovePick, RollContext, out string)"/> that never removes <paramref name="keepId"/>.</summary>
        public static RollOutcome RemoveOne(ItemState current, RemovePick pick, RollContext context, string? keepId,
            out string? removedId)
        {
            RollOutcome outcome = RollOps.RemoveOne(current, pick, context, keepId, out removedId);
            return RollLog.Report("remove " + pick, current, outcome, context);
        }

        /// <summary>
        /// Turmoil's swap (stones.md section 9): removes one affix (never the bound one or <paramref name="keepId"/>),
        /// then rolls a different affix of the same pool class into its position (a Mythic-only one from the
        /// Mythic-only pool, regular pool as fallback). The count never changes. Fails with
        /// <see cref="RollFailure.AtMinimum"/> when nothing is removable, <see cref="RollFailure.NoEligibleAffix"/>
        /// when no replacement other than the removed id can roll.
        /// </summary>
        public static RollOutcome Swap(ItemState current, RemovePick pick, RollContext context, string? keepId,
            out string? removedId, out string? addedId)
        {
            RollOutcome outcome = RollOps.Swap(current, pick, context, keepId, out removedId, out addedId);
            return RollLog.Report("swap " + pick, current, outcome, context);
        }

        // ---- Phase 2 additions by the Stones area (additive)

        /// <summary>
        /// Upheaval (stones.md section 10): <see cref="RollFresh"/> that also keeps <paramref name="keepId"/> (the
        /// affix a Sigil of Preservation protects) beside the bound one. Kept affixes count toward the drawn count; a
        /// kept Mythic-only affix fills the special slot. <see cref="RollFailure.NoEligibleAffix"/> below the minimum.
        /// </summary>
        public static RollOutcome Reroll(ItemState current, RarityDef rarity, RollContext context, string? keepId)
        {
            return RollLog.Report("reroll " + rarity.Id, current, RollOps.RollFresh(current, rarity, context, keepId), context);
        }

        /// <summary>
        /// An essence (essences.md section 4, verb <c>imbue</c>): Upheaval that guarantees one new affix from
        /// <see cref="ImbueRequest.Family"/>. The bound affix and <see cref="ImbueRequest.KeepId"/> stay; every other affix
        /// goes, dormant ones included; the count is drawn as a fresh roll, raised to the kept count + 1. The family affix is
        /// drawn first at tiers from <c>max(window low, family floor)</c> to the ceiling, then the Mythic-only slot, then the
        /// fill with the plain window (family members allowed, ESS-15). A kept family member does not count as the
        /// guarantee (ESS-6). Fails with <see cref="RollFailure.NoFamilyMatch"/> when no member is eligible,
        /// <see cref="RollFailure.NoEligibleAffix"/> below the rarity's minimum. The caller checks that kept + 1 fits.
        /// </summary>
        public static RollOutcome Imbue(ItemState current, RarityDef rarity, RollContext context, ImbueRequest request,
            out string? guaranteedId)
        {
            RollOutcome outcome = ImbueOps.Imbue(current, rarity, context, request, out guaranteedId);
            return RollLog.Report("imbue " + rarity.Id, current, outcome, context);
        }

        /// <summary>
        /// The Serpent's chaotic reroll (stones.md section 14): every affix goes, the binding with it; the count is
        /// drawn in the rarity's range and every affix rolls at any tier it defines, uniformly, ignoring ceiling, window
        /// and floor (item-tier.md section 6). Fills what the pool can; fails only when it can fill nothing.
        /// </summary>
        public static RollOutcome RollChaotic(ItemState current, RarityDef rarity, RollContext context)
        {
            return RollLog.Report("chaotic " + rarity.Id, current, CorruptOps.Chaotic(current, rarity, context), context);
        }

        /// <summary>
        /// One rarity down to <paramref name="to"/> (rarity.md section 3): Mythic-only affixes are stripped when leaving
        /// a rarity with <c>mythic_affixes</c>, otherwise one random affix is lost (the bound one eligible); then the
        /// new maximum is enforced, and the base rarity holds none. Never fails.
        /// </summary>
        public static RollOutcome Demote(ItemState current, RarityDef to, RollContext context)
        {
            return RollLog.Report("demote " + to.Id, current, CorruptOps.Demote(current, to, context), context);
        }
    }
}
