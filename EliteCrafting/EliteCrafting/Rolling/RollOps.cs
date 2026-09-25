using System;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The bodies behind <see cref="ItemRoller"/>. Every operation copies the state into a builder, works on the copy
    /// and returns it built, or a failure with nothing changed; nothing here writes an item. Runs on whichever peer
    /// owns the item (the clicking client for a stone, the creature's ZDO owner for a drop) under the synced rules.
    /// </summary>
    internal static class RollOps
    {
        public static RollOutcome RollFresh(ItemState current, RarityDef rarity, RollContext context) =>
            RollFresh(current, rarity, context, null);

        /// <summary>
        /// A fresh roll at <paramref name="rarity"/> that keeps the bound affix and <paramref name="keepId"/> (Upheaval
        /// with Preservation) in their order and counts them toward the drawn count; everything else is replaced.
        /// </summary>
        public static RollOutcome RollFresh(ItemState current, RarityDef rarity, RollContext context, string? keepId)
        {
            ItemStateBuilder builder = current.ToBuilder();
            KeepOnly(builder, current, keepId);
            builder.SetRarity(rarity.Id);
            context.Rules.Economy.Rolling.CountWeights.TryGetValue(rarity.Id, out var weights);
            int count = RollMath.PickCount(rarity.MinAffixes, rarity.MaxAffixes, weights, context.Random);
            AffixDraw draw = new AffixDraw(builder, context);
            int special = Math.Max(0, rarity.MythicAffixes - MythicCount(builder, context));
            Fill(draw, count, special, allOrNothing: false);
            return builder.AffixCount >= rarity.MinAffixes ? RollOutcome.Ok(builder.Build()) : Fail(RollFailure.NoEligibleAffix);
        }

        public static RollOutcome AddAffixes(ItemState current, int count, RollContext context)
        {
            ItemStateBuilder builder = current.ToBuilder();
            AffixDraw draw = new AffixDraw(builder, context);
            return Fill(draw, builder.AffixCount + count, 0, allOrNothing: true)
                ? RollOutcome.Ok(builder.Build())
                : Fail(RollFailure.NoEligibleAffix);
        }

        /// <summary>
        /// One rarity up (rarity.md section 3): every affix kept; adds <c>max(new.min - count, at_least)</c> capped at
        /// the new maximum, the Mythic-only ones first. All or nothing.
        /// </summary>
        public static RollOutcome Promote(ItemState current, RarityDef to, RollContext context)
        {
            ItemStateBuilder builder = current.ToBuilder();
            builder.SetRarity(to.Id);
            int count = builder.AffixCount;
            int atLeast = context.Rules.Economy.Rolling.PromoteAddsAtLeast;
            int added = Math.Min(Math.Max(to.MinAffixes - count, atLeast), Math.Max(to.MaxAffixes - count, 0));
            int special = Math.Max(0, to.MythicAffixes - MythicCount(builder, context));
            AffixDraw draw = new AffixDraw(builder, context);
            return Fill(draw, count + added, special, allOrNothing: true)
                ? RollOutcome.Ok(builder.Build())
                : Fail(RollFailure.NoEligibleAffix);
        }

        /// <summary>Perfection: live, unbound, numeric affixes whose stored tier still exists; <paramref name="keepId"/> skipped.</summary>
        public static RollOutcome RerollValues(ItemState current, RollContext context, string? keepId)
        {
            ItemStateBuilder builder = current.ToBuilder();
            int rerolled = 0;
            for (int i = 0; i < current.AffixCount; i++)
            {
                AffixTierDef? row = RerollRow(current, i, keepId);
                if (row == null)
                {
                    continue;
                }
                AffixRoll roll = current.Affixes[i];
                builder.ReplaceAffix(roll.Id, roll.WithValue(RollMath.RollValue(row.Min, row.Max, row.Decimals, context.Random)));
                rerolled++;
            }
            return rerolled == 0 ? Fail(RollFailure.NothingToReroll) : RollOutcome.Ok(builder.Build());
        }

        /// <summary>The tier row a value reroll uses, or null when the affix is not rerollable (stones.md section 11).</summary>
        public static AffixTierDef? RerollRow(ItemState state, int index, string? keepId)
        {
            AffixDef? def = state.DefinitionAt(index);
            AffixRoll roll = state.Affixes[index];
            if (def == null || !state.IsActiveAt(index) || state.IsBoundAt(index) || roll.Id == keepId)
            {
                return null;
            }
            return def.Value == AffixValueType.Flag ? null : def.TierRow(roll.Tier);
        }

        public static RollOutcome RemoveOne(ItemState current, RemovePick pick, RollContext context, string? keepId,
            out string? removedId)
        {
            int index = pick == RemovePick.LowestTier ? AffixRanking.Culled(current, keepId) : RandomRemovable(current, keepId, context);
            removedId = index < 0 ? null : current.Affixes[index].Id;
            if (removedId == null)
            {
                return Fail(RollFailure.AtMinimum);
            }
            ItemStateBuilder builder = current.ToBuilder();
            builder.RemoveAffix(removedId);
            return RollOutcome.Ok(builder.Build());
        }

        /// <summary>
        /// Turmoil (stones.md section 9): removes one (never the bound one or <paramref name="keepId"/>) and rolls a
        /// different affix of the same pool class into its position; the removed id is excluded from the draw.
        /// </summary>
        public static RollOutcome Swap(ItemState current, RemovePick pick, RollContext context, string? keepId,
            out string? removedId, out string? addedId)
        {
            addedId = null;
            RollOutcome removal = RemoveOne(current, pick, context, keepId, out removedId);
            if (!removal.Success)
            {
                return removal;
            }
            bool special = context.Rules.Affixes.Get(removedId)?.MythicOnly == true;
            if (!DrawReplacement(removal.State!, context, removedId!, special, out AffixRoll roll))
            {
                return Fail(RollFailure.NoEligibleAffix);
            }
            ItemStateBuilder builder = current.ToBuilder();
            builder.ReplaceAffix(removedId!, roll);
            addedId = roll.Id;
            return RollOutcome.Ok(builder.Build());
        }

        private static bool DrawReplacement(ItemState without, RollContext context, string removedId, bool special,
            out AffixRoll roll)
        {
            string? previous = context.ExcludeId;
            context.ExcludeId = removedId;
            try
            {
                AffixDraw draw = new AffixDraw(without.ToBuilder(), context);
                return (special && draw.TryDraw(true, out roll)) || draw.TryDraw(false, out roll);
            }
            finally
            {
                context.ExcludeId = previous;
            }
        }

        // Draws until the item holds `target` affixes, the first `special` from the Mythic-only pool (regular fallback).
        internal static bool Fill(AffixDraw draw, int target, int special, bool allOrNothing)
        {
            while (draw.Builder.AffixCount < target)
            {
                bool added = special > 0 ? draw.TryAddSpecial() : draw.TryAdd(false);
                special--;
                if (!added)
                {
                    return !allOrNothing;
                }
            }
            return true;
        }

        // Clears every affix but the bound one and keepId, which stay in their order (the binding stays on its affix).
        internal static void KeepOnly(ItemStateBuilder builder, ItemState current, string? keepId)
        {
            builder.ClearAffixes();
            for (int i = 0; i < current.AffixCount; i++)
            {
                if (current.IsBoundAt(i) || current.Affixes[i].Id == keepId)
                {
                    builder.AddAffix(current.Affixes[i]);
                }
            }
            if (current.BoundId != null && builder.HasAffix(current.BoundId))
            {
                builder.SetBound(current.BoundId);
            }
        }

        internal static int MythicCount(ItemStateBuilder builder, RollContext context)
        {
            int count = 0;
            foreach (AffixRoll roll in builder.Affixes)
            {
                if (context.Rules.Affixes.Get(roll.Id)?.MythicOnly == true)
                {
                    count++;
                }
            }
            return count;
        }

        private static int RandomRemovable(ItemState state, string? keepId, RollContext context)
        {
            int removable = 0;
            for (int i = 0; i < state.AffixCount; i++)
            {
                removable += AffixRanking.Removable(state, i, keepId) ? 1 : 0;
            }
            int pick = removable == 0 ? -1 : context.Random.Next(removable);
            for (int i = 0; i < state.AffixCount && pick >= 0; i++)
            {
                if (AffixRanking.Removable(state, i, keepId) && pick-- == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private static RollOutcome Fail(RollFailure failure) => RollOutcome.Fail(failure);
    }
}
