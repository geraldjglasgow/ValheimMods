using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>One roll that counts for effects: an active inscription with its live definition.</summary>
    public readonly struct EffectRoll
    {
        public EffectRoll(AffixRoll roll, AffixDef def)
        {
            Roll = roll;
            Def = def;
        }

        public AffixRoll Roll { get; }
        public AffixDef Def { get; }
    }

    /// <summary>
    /// Builds an item state's <see cref="ItemState.EffectRolls"/> once, when the state is resolved: the active affixes
    /// in stored order. Dormant affixes count for nothing.
    /// </summary>
    internal static class EffectRollBuilder
    {
        public static EffectRoll[] Build(ItemState state)
        {
            if (state.AffixCount == 0)
            {
                return Array.Empty<EffectRoll>();
            }
            List<EffectRoll> rolls = new List<EffectRoll>(state.AffixCount);
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (state.IsActiveAt(i))
                {
                    rolls.Add(new EffectRoll(state.Affixes[i], state.DefinitionAt(i)!));
                }
            }
            return rolls.ToArray();
        }
    }
}
