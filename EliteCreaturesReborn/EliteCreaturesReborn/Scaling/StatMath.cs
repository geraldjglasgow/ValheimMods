using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// Combines a creature's star power and its mutations into the fixed multipliers - health, size, movement, swing
    /// speed - the way the specification demands: additively, never multiplicatively. Every multiplier is read as a
    /// bonus above 1, the bonuses are summed, and the total is applied once, so two mutations pulling opposite ways
    /// cancel in proportion rather than compounding into an absurd number.
    /// </summary>
    public static class StatMath
    {
        /// <summary>The one hard floor: a combined health multiplier below this would be a crash, not a fight.</summary>
        public const float HealthFloor = 0.05f;

        /// <summary>A correctness floor so a very well-fed Devouring creature crawls rather than reverses or freezes.</summary>
        public const float MoveFloor = 0.05f;

        public static float HealthMultiplier(BiomeRules rules, CreatureTraits traits)
        {
            float bonus = rules.Star.HpAt(traits.Stars) - 1f;
            if (traits.Has(Mutation.Mad))
            {
                bonus += rules.PowerOf(Mutation.Mad, Fields.Health) - 1f; // a cost: never enhanced
            }
            if (traits.Has(Mutation.Bloated))
            {
                bonus += Enhance.Stat(rules, traits, Mutation.Bloated, Fields.Health) - 1f;
            }
            return Mathf.Max(HealthFloor, 1f + bonus);
        }

        public static float SizeMultiplier(BiomeRules rules, CreatureTraits traits)
        {
            return 1f + rules.Star.GrowthAt(traits.Stars);
        }

        /// <summary>Movement, including a live negative bonus for Devouring's accumulated bulk (0 when it carries none).</summary>
        public static float MoveMultiplier(BiomeRules rules, CreatureTraits traits, float devourBonus)
        {
            float bonus = rules.Star.SpeedAt(traits.Stars) - 1f + devourBonus;
            if (traits.Has(Mutation.Mad))
            {
                bonus += Enhance.Stat(rules, traits, Mutation.Mad, Fields.Move) - 1f;
            }
            if (traits.Has(Mutation.Devouring))
            {
                bonus += rules.PowerOf(Mutation.Devouring, Fields.Move) - 1f; // a tuning knob, usually <= 1: never enhanced
            }
            return Mathf.Max(MoveFloor, 1f + bonus);
        }

        public static float SwingSpeedMultiplier(BiomeRules rules, CreatureTraits traits)
        {
            float bonus = rules.Star.SwingSpeedAt(traits.Stars) - 1f;
            if (traits.Has(Mutation.Mad))
            {
                bonus += Enhance.Stat(rules, traits, Mutation.Mad, Fields.AttackSpeed) - 1f;
            }
            return Mathf.Max(MoveFloor, 1f + bonus);
        }
    }
}
