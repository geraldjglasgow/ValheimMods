using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// One roll that counts for effects: an active affix or the affix of a socketed gem, with its value already
    /// multiplied by the item's catalyst when the affix belongs to the catalyst's family (sockets.md section 6).
    /// </summary>
    public readonly struct EffectRoll
    {
        public EffectRoll(AffixRoll roll, AffixDef def, bool fromGem)
        {
            Roll = roll;
            Def = def;
            FromGem = fromGem;
        }

        public AffixRoll Roll { get; }
        public AffixDef Def { get; }
        public bool FromGem { get; }
    }

    /// <summary>
    /// Builds an item state's <see cref="ItemState.EffectRolls"/> once, when the state is resolved: the active affixes
    /// in stored order, then the gems whose affix is defined and enabled, oldest first. Dormant affixes and gems count
    /// for nothing. Flags are never scaled by a catalyst.
    /// </summary>
    internal static class EffectRollBuilder
    {
        public static EffectRoll[] Build(ItemState state, AffixDef?[] gemDefs)
        {
            if (state.AffixCount == 0 && gemDefs.Length == 0)
            {
                return Array.Empty<EffectRoll>();
            }
            List<EffectRoll> rolls = new List<EffectRoll>(state.AffixCount + gemDefs.Length);
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (state.IsActiveAt(i))
                {
                    rolls.Add(Scaled(state, state.Affixes[i], state.DefinitionAt(i)!, false));
                }
            }
            for (int i = 0; i < gemDefs.Length; i++)
            {
                if (gemDefs[i] != null && gemDefs[i]!.Enabled)
                {
                    rolls.Add(Scaled(state, state.Gems[i].Roll, gemDefs[i]!, true));
                }
            }
            return rolls.ToArray();
        }

        private static EffectRoll Scaled(ItemState state, AffixRoll roll, AffixDef def, bool gem)
        {
            float factor = def.Value == AffixValueType.Flag ? 1f : state.CatalystFactor(roll.Id);
            return new EffectRoll(factor == 1f ? roll : roll.WithValue(roll.Value * factor), def, gem);
        }
    }
}
