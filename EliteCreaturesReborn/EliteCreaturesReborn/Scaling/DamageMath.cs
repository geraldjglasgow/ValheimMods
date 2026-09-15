using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// The damage modifiers computed at the moment of a hit, where health-linked mutations need the current state.
    /// Outgoing damage sums star attack, Splintering's penalty and Plated's rising bite additively; Devouring adds a
    /// flat amount from what it has eaten; incoming damage is softened by Plated's armour, which fades as it is hurt.
    /// The armour uses the game's own curve so a plated creature reads the way any armoured one does.
    /// </summary>
    public static class DamageMath
    {
        public static float OutgoingMultiplier(BiomeRules rules, CreatureTraits traits, float healthFraction)
        {
            float bonus = rules.Star.AttackAt(traits.Stars) - 1f;
            if (traits.Has(Mutation.Splintering))
            {
                bonus += rules.PowerOf(Mutation.Splintering, Fields.Damage) - 1f; // not enhanced
            }
            if (traits.Has(Mutation.Plated))
            {
                bonus += Enhance.Magnitude(rules, traits, Mutation.Plated, Fields.Damage) / 100f * (1f - healthFraction);
            }
            return Mathf.Max(0f, 1f + bonus);
        }

        /// <summary>Flat physical damage a Devouring creature adds from the damage of what it has eaten.</summary>
        public static float DevouredFlatDamage(CreatureTraits traits, ZDO zdo)
        {
            if (!traits.Has(Mutation.Devouring) || zdo == null)
            {
                return 0f;
            }
            return TraitStore.GetDevouredDamage(zdo);
        }

        /// <summary>Armour points a Plated victim gets, full at full health and fading to nothing at empty; 0 otherwise.</summary>
        public static float PlatedArmour(BiomeRules rules, CreatureTraits traits, float healthFraction)
        {
            if (!traits.Has(Mutation.Plated))
            {
                return 0f;
            }
            return Enhance.Magnitude(rules, traits, Mutation.Plated, Fields.Armour) * Mathf.Clamp01(healthFraction);
        }
    }
}
