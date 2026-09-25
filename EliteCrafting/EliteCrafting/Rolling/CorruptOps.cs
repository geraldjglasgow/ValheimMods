using System;
using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The two ladder moves only the Serpent Stone makes by default (stones.md section 14, rarity.md section 3): the
    /// chaotic reroll and the demotion. Both are pure like the rest of <see cref="ItemRoller"/>: a copy is changed and
    /// returned, nothing is written. Runs on the client that owns the item, under the synced rules.
    /// </summary>
    internal static class CorruptOps
    {
        /// <summary>
        /// Removes every affix (bound and dormant included; the binding goes with them), draws the count in the
        /// rarity's range and rolls each affix at any tier it defines, uniformly, ignoring ceiling, window and floor.
        /// Fills what the pool can; fails only when it can fill none of a count above 0. Unreadable segments stay.
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
                context.Rules.Economy.Rolling.CountWeights.TryGetValue(rarity.Id, out var weights);
                int count = RollMath.PickCount(rarity.MinAffixes, rarity.MaxAffixes, weights, context.Random);
                RollOps.Fill(new AffixDraw(builder, context), count, rarity.MythicAffixes, allOrNothing: false);
                return builder.AffixCount > 0 || count == 0
                    ? RollOutcome.Ok(builder.Build())
                    : RollOutcome.Fail(RollFailure.NoEligibleAffix);
            }
            finally
            {
                context.Chaotic = wasChaotic;
            }
        }

        /// <summary>
        /// One rarity down to <paramref name="to"/> (rarity.md section 3, demote): leaving a rarity with
        /// <c>mythic_affixes</c> strips every Mythic-only affix, which counts as the lost affix; otherwise one random
        /// affix goes (the bound one is eligible). Then random affixes go until the new maximum fits, and the base
        /// rarity keeps none. Never fails.
        /// </summary>
        public static RollOutcome Demote(ItemState current, RarityDef to, RollContext context)
        {
            ItemStateBuilder builder = current.ToBuilder();
            RarityDef? from = context.Rules.Rarity(current.RarityId);
            int stripped = from != null && from.MythicAffixes > 0 ? RemoveMythicOnly(builder, context) : 0;
            if (stripped == 0)
            {
                RemoveRandom(builder, context.Random);
            }
            while (builder.AffixCount > Math.Max(to.MaxAffixes, 0))
            {
                RemoveRandom(builder, context.Random);
            }
            if (to.IsBase)
            {
                builder.ClearAffixes();
            }
            builder.SetRarity(to.Id);
            return RollOutcome.Ok(builder.Build());
        }

        private static int RemoveMythicOnly(ItemStateBuilder builder, RollContext context)
        {
            int removed = 0;
            foreach (AffixRoll roll in builder.Affixes)
            {
                if (context.Rules.Affixes.Get(roll.Id)?.MythicOnly == true && builder.RemoveAffix(roll.Id))
                {
                    removed++;
                }
            }
            return removed;
        }

        // Any affix, bound and dormant ones included: the Serpent is one of the two things that may remove a binding.
        private static void RemoveRandom(ItemStateBuilder builder, Random random)
        {
            List<AffixRoll> affixes = builder.Affixes;
            if (affixes.Count > 0)
            {
                builder.RemoveAffix(affixes[random.Next(affixes.Count)].Id);
            }
        }
    }
}
