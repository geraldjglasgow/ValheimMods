using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The roll a gem gives an item (sockets.md section 4): the affix the gem names for the item's slot, a tier from the
    /// item's ordinary window (<see cref="TierWindow"/>: ceiling, window, no floor), a value uniform in that tier's
    /// range. Pure and read-only, like every roll; the stone writes the result.
    /// </summary>
    internal static class GemRolls
    {
        /// <summary>The roll, or null when the affix defines no tier this item can roll.</summary>
        public static AffixRoll? Roll(AffixDef def, RollContext context)
        {
            List<AffixTierDef> rows = new List<AffixTierDef>();
            TierWindow.Eligible(def, context, rows);
            if (rows.Count == 0)
            {
                return null;
            }
            AffixTierDef row = TierWindow.Pick(rows, context, new List<float>());
            float value = def.Value == AffixValueType.Flag ? 1f : RollMath.RollValue(row.Min, row.Max, row.Decimals, context.Random);
            return new AffixRoll(def.Id, row.Tier, value);
        }
    }
}
