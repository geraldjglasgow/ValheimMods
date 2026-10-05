using System;
using System.Collections.Generic;
using EliteCrafting.Affixes;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The Recasting Rune's reroll (user decision 2026-10-04): between one and all of the item's affixes are replaced
    /// in place by new draws, the rest stay, so the item never loses an affix. Pure like the rest of
    /// <see cref="ItemRoller"/>: a copy is changed and returned, nothing is written.
    /// </summary>
    internal static class RecastOps
    {
        /// <summary>
        /// Picks the affixes to replace (<see cref="Pick"/>), draws as many new ones with only the kept ones on the item
        /// (so a replaced affix may come back; a replaced one frees its kind's place first, and the new one may be either
        /// kind within the rarity's limits) and puts each where the old one stood. All or nothing: fails when the
        /// item holds no affix or the pool cannot replace every pick. Unreadable segments stay.
        /// </summary>
        public static RollOutcome Recast(ItemState current, RollContext context)
        {
            List<AffixRoll> rolls = current.ToBuilder().Affixes;
            if (rolls.Count == 0)
            {
                return RollOutcome.Fail(RollFailure.NoEligibleAffix);
            }
            bool[] picked = Pick(rolls.Count, context.Random);
            List<AffixRoll>? fresh = Draw(current, rolls, picked, context);
            return fresh == null
                ? RollOutcome.Fail(RollFailure.NoEligibleAffix)
                : RollOutcome.Ok(Place(current, rolls, picked, fresh));
        }

        /// <summary>
        /// Which of <paramref name="count"/> places to reroll: how many uniformly in 1..count, then which uniformly (a
        /// partial shuffle).
        /// </summary>
        private static bool[] Pick(int count, Random random)
        {
            int[] order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }
            bool[] picked = new bool[count];
            int take = random.Next(1, count + 1);
            for (int i = 0; i < take; i++)
            {
                int swap = random.Next(i, count);
                (order[i], order[swap]) = (order[swap], order[i]);
                picked[order[i]] = true;
            }
            return picked;
        }

        // The new affixes, in draw order; null when the pool runs out before every pick is replaced.
        private static List<AffixRoll>? Draw(ItemState current, List<AffixRoll> rolls, bool[] picked, RollContext context)
        {
            ItemStateBuilder scratch = current.ToBuilder();
            for (int i = 0; i < rolls.Count; i++)
            {
                if (picked[i])
                {
                    scratch.RemoveAffix(rolls[i].Id);
                }
            }
            int kept = scratch.AffixCount;
            if (!RollOps.Fill(new AffixDraw(scratch, context), rolls.Count, allOrNothing: true))
            {
                return null;
            }
            return scratch.Affixes.GetRange(kept, rolls.Count - kept);
        }

        // The item with each picked affix swapped for the next new one, at the old one's place.
        private static ItemState Place(ItemState current, List<AffixRoll> rolls, bool[] picked, List<AffixRoll> fresh)
        {
            ItemStateBuilder builder = current.ToBuilder();
            builder.ClearAffixes();
            int next = 0;
            for (int i = 0; i < rolls.Count; i++)
            {
                builder.AddAffix(picked[i] ? fresh[next++] : rolls[i]);
            }
            return builder.Build();
        }
    }
}
