using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Piercing: each of its hits on a player - melee, thrown or shot - meets only part of the player's armour: the game
    /// weighs the hit against their armour with <see cref="Ignored"/> of it taken away, a share a large star enhances.
    /// Block, parry, resistances and the armour's wear are untouched. Applied on the struck player's own machine, where
    /// the game resolves the hit (<c>Patches.PiercingPatch</c>).
    /// </summary>
    internal static class Piercing
    {
        /// <summary>The share of a player's armour its hits ignore.</summary>
        public const float Ignored = 0.15f;

        /// <summary>The share of armour this creature's hits ignore, 0 to 1.</summary>
        public static float Share(BiomeRules rules, CreatureTraits traits)
        {
            float gain = traits.OnLargeStar(Mutation.Piercing) ? rules.LargeStarPower : 1f;
            return Mathf.Clamp01(Ignored * gain);
        }
    }
}
