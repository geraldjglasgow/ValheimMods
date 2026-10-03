using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The one affix-drawing procedure of the mod (rarity.md section 4): every rune that adds affixes, pre-rolled
    /// drops and <c>ecraft roll</c> call these. Every method is pure: it takes a state, returns a new state or a
    /// failure, and never writes the item; the caller commits with <see cref="ItemState.Write"/>. Rolls run on the peer
    /// that owns the item, under the synced rules.
    /// <para>
    /// Shared rules for every method: candidates are enabled affixes with weight above 0 whose slots contain the
    /// item's slot, that pass <c>requires</c> (<c>Items.ItemSlots.Satisfies</c>), are not on the item (dormant copies
    /// included), share no exclusion group with an affix on the item, and have an eligible tier. Tier window:
    /// <c>low = max(ceiling - rolling.tier_window + 1, tier_floor, 1)</c>, <c>high = ceiling</c>; an affix with no
    /// tier in the window but tiers below it is eligible at its highest tier at or below the ceiling (if at least the
    /// floor). Draw: affix by <c>weight</c>, then tier by tier <c>weight</c>, then value uniformly in [min, max]
    /// rounded to the tier's decimals (flags store 1). Chaotic rolls use every tier the affix defines, uniformly.
    /// </para>
    /// </summary>
    public static class ItemRoller
    {
        /// <summary>
        /// Rolls an item fresh at <paramref name="rarity"/> (pre-rolled drops, <c>ecraft roll</c>): every affix is
        /// replaced; the count is drawn in [min, max] of the rarity (uniform, or <c>rolling.count_weights</c>). Sets
        /// the rarity. Fails with <see cref="RollFailure.NoEligibleAffix"/> when the pool cannot reach the rarity's
        /// minimum (drops then fall back to a lower rarity themselves).
        /// </summary>
        public static RollOutcome RollFresh(ItemState current, RarityDef rarity, RollContext context)
        {
            return RollLog.Report("fresh " + rarity.Id, current, RollOps.RollFresh(current, rarity, context), context);
        }

        /// <summary>
        /// Adds exactly <paramref name="count"/> affixes at the end of the list (Shaping, Consecrated, the Serpent's
        /// add). Does not change the rarity and does not check the rarity's maximum (the caller decides whether the
        /// item has room). All or nothing: fails with <see cref="RollFailure.NoEligibleAffix"/> when fewer candidates
        /// exist than needed.
        /// </summary>
        public static RollOutcome AddAffixes(ItemState current, int count, RollContext context)
        {
            return RollLog.Report("add " + count, current, RollOps.AddAffixes(current, count, context), context);
        }

        /// <summary>
        /// Promotes one rarity up to <paramref name="to"/> (Awakening, Ascension; rarity.md section 3): every affix
        /// kept, dormant included; adds <c>max(to.min - count, rolling.promote_adds_at_least)</c> capped at
        /// <c>to.max</c>. All or nothing: <see cref="RollFailure.NoEligibleAffix"/> when the pool cannot supply them.
        /// Sets the rarity.
        /// </summary>
        public static RollOutcome Promote(ItemState current, RarityDef to, RollContext context)
        {
            return RollLog.Report("promote " + to.Id, current, RollOps.Promote(current, to, context), context);
        }

        /// <summary>
        /// The Serpent Rune's chaotic reroll: every affix goes; the count is drawn in the rarity's range and every
        /// affix rolls at any tier it defines, uniformly, ignoring ceiling, window and floor (item-tier.md section 6).
        /// Fills what the pool can; fails only when it can fill nothing.
        /// </summary>
        public static RollOutcome RollChaotic(ItemState current, RarityDef rarity, RollContext context)
        {
            return RollLog.Report("chaotic " + rarity.Id, current, CorruptOps.Chaotic(current, rarity, context), context);
        }
    }
}
