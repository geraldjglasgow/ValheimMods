using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The Serpent Rune's chaotic reroll (rarity.md section 4). Pure like the rest of <see cref="ItemRoller"/>: a copy
    /// is changed and returned, nothing is written. Runs on the client that owns the item, under the synced rules.
    /// </summary>
    internal static class CorruptOps
    {
        /// <summary>
        /// Removes every affix (dormant ones included), draws the count in the rarity's range and rolls each affix at
        /// any tier it defines, uniformly, ignoring the item level, the closed tiers and the floor (the class pool and the
        /// prefix and suffix limits still apply). Fills what the pool can; fails only when
        /// it can fill none of a count above 0. Unreadable segments stay.
        /// </summary>
        public static RollOutcome Chaotic(ItemState current, RarityDef rarity, RollContext context)
        {
            ItemStateBuilder builder = current.ToBuilder();
            builder.ClearAffixes();
            builder.SetRarity(rarity.Id);
            bool wasChaotic = context.Chaotic;
            context.Chaotic = true;
            try
            {
                int count = RollOps.DrawCount(rarity, context);
                RollOps.Fill(new AffixDraw(builder, context), count, allOrNothing: false);
                return builder.AffixCount > 0 || count == 0
                    ? RollOutcome.Ok(builder.Build())
                    : RollOutcome.Fail(RollFailure.NoEligibleAffix);
            }
            finally
            {
                context.Chaotic = wasChaotic;
            }
        }
    }
}
