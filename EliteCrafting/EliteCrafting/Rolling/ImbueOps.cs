using System;
using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The body of <see cref="ItemRoller.Imbue"/> (essences.md section 4, step 3): Upheaval with one guaranteed affix
    /// from a family. Pure: works on a copy and never writes. Runs on the client that owns the item, under the synced
    /// rules, like every stone roll.
    /// </summary>
    internal static class ImbueOps
    {
        public static RollOutcome Imbue(ItemState current, RarityDef rarity, RollContext context, ImbueRequest request,
            out string? guaranteedId)
        {
            ItemStateBuilder builder = current.ToBuilder();
            RollOps.KeepOnly(builder, current, request.KeepId);
            builder.SetRarity(rarity.Id);
            int count = Math.Max(DrawCount(rarity, context), builder.AffixCount + 1);
            AffixDraw draw = new AffixDraw(builder, context);
            if (!DrawGuaranteed(draw, context, request, out guaranteedId))
            {
                return RollOutcome.Fail(RollFailure.NoFamilyMatch);
            }
            int special = Math.Max(0, rarity.MythicAffixes - RollOps.MythicCount(builder, context));
            RollOps.Fill(draw, count, special, allOrNothing: false);
            return builder.AffixCount >= rarity.MinAffixes
                ? RollOutcome.Ok(builder.Build())
                : RollOutcome.Fail(RollFailure.NoEligibleAffix);
        }

        // 3.2: as a fresh roll draws it (uniform in [min, max], or rolling.count_weights).
        private static int DrawCount(RarityDef rarity, RollContext context)
        {
            context.Rules.Economy.Rolling.CountWeights.TryGetValue(rarity.Id, out var weights);
            return RollMath.PickCount(rarity.MinAffixes, rarity.MaxAffixes, weights, context.Random);
        }

        // 3.3: the family draw first, from the regular pool restricted to the members, with the essence's floor (clamped
        // to the ceiling by the tier window). The fill after it uses the plain window: the floor is restored here.
        private static bool DrawGuaranteed(AffixDraw draw, RollContext context, ImbueRequest request, out string? id)
        {
            int floor = context.TierFloor;
            context.TierFloor = request.FamilyFloor;
            draw.Only = new HashSet<string>(request.Family, StringComparer.Ordinal);
            try
            {
                bool drawn = draw.TryDraw(false, out AffixRoll roll);
                id = drawn ? roll.Id : null;
                if (drawn)
                {
                    draw.Builder.AddAffix(roll);
                    draw.Occupy(roll.Id);
                }
                return drawn;
            }
            finally
            {
                context.TierFloor = floor;
                draw.Only = null;
            }
        }
    }

    /// <summary>What an essence asks of <see cref="ItemRoller.Imbue"/>: the family's member ids, its floor, the preserved affix.</summary>
    public sealed class ImbueRequest
    {
        public ImbueRequest(IReadOnlyList<string> family, int familyFloor, string? keepId)
        {
            Family = family;
            FamilyFloor = familyFloor;
            KeepId = keepId;
        }

        /// <summary>Affix ids the guaranteed draw may pick; ineligible ones are skipped like any candidate.</summary>
        public IReadOnlyList<string> Family { get; }

        /// <summary>The lowest tier of the guaranteed affix (0 = none), clamped to the ceiling; the fill has no floor.</summary>
        public int FamilyFloor { get; }

        /// <summary>The affix a Sigil of Preservation protects, kept beside the bound one; null = none.</summary>
        public string? KeepId { get; }
    }
}
