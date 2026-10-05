using System;
using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Core;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The <c>Log rolls</c> diagnostic (configuration.md section 2, per player): every roll through
    /// <see cref="ItemRoller"/> with its inputs and result, whoever asked for it (stone, drop, command). Costs one
    /// setting read when off.
    /// </summary>
    internal static class RollLog
    {
        public static RollOutcome Report(string what, ItemState before, RollOutcome outcome, RollContext context)
        {
            if (ModSettings.LogRolls == null || !ModSettings.LogRolls.Value)
            {
                return outcome;
            }
            string inputs = $"class {context.Class.ClassId ?? "none"}, level {context.Level}, floor {context.TierFloor}"
                + (context.Chaotic ? ", chaotic" : "") + (context.LimitOverflow > 0 ? $", overflow {context.LimitOverflow}" : "");
            string result = outcome.Success ? Describe(outcome.State!) : "failed: " + outcome.Failure;
            Log.Info($"roll {what} ({inputs}): {Describe(before)} -> {result}");
            return outcome;
        }

        private static string Describe(ItemState state)
        {
            StringBuilder text = new StringBuilder(state.RarityId ?? "normal");
            text.Append(" [");
            for (int i = 0; i < state.AffixCount; i++)
            {
                text.Append(i == 0 ? "" : ", ").Append(state.Affixes[i]);
            }
            return text.Append(']').ToString();
        }
    }
}
