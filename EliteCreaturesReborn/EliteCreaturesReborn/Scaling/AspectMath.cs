using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// The numbers an aspect changes about the boss itself, read live from the rules. They multiply the boss as its
    /// stars left it rather than joining the additive star sum, so "25% less health" means exactly that - an aspect is
    /// a separate layer, not another line on the star table. Hit-time damage changes live in <see cref="AspectDamage"/>.
    /// </summary>
    public static class AspectMath
    {
        /// <summary>The floor under a reduced multiplier, so a misconfigured 100% cut leaves a fight rather than a crash.</summary>
        private const float Floor = 0.05f;

        public static float Power(Aspect aspect, string field) => RuleState.Active.Boss.Aspects.PowerOf(aspect, field);

        /// <summary>Twin's cut to the starred maximum health; 1 for everything else (a Phantom copy's health is flat).</summary>
        public static float HealthFactor(CreatureTraits traits) =>
            traits.Aspect == Aspect.Twin ? Cut(Power(Aspect.Twin, Fields.LessHealth)) : 1f;

        /// <summary>A Phantom copy's whole maximum health, whatever its stars: a copy falls to about one blow.</summary>
        public static float PhantomHealth() => Mathf.Max(1f, Power(Aspect.Phantom, Fields.Health));

        /// <summary>The share of its starred damage a boss deals: Twin's and a Phantom copy's cut, 1 otherwise.</summary>
        public static float DamageFactor(CreatureTraits traits)
        {
            if (traits.PhantomCopy)
            {
                return Cut(Power(Aspect.Phantom, Fields.LessDamage));
            }
            return traits.Aspect == Aspect.Twin ? Cut(Power(Aspect.Twin, Fields.LessDamage)) : 1f;
        }

        /// <summary>A "percent less" number as the multiplier it leaves: 25 becomes 0.75.</summary>
        public static float Cut(float percentLess) => Mathf.Max(Floor, 1f - percentLess / 100f);

        /// <summary>A "percent more" number as its multiplier: 20 becomes 1.2.</summary>
        public static float Boost(float percentMore) => Mathf.Max(0f, 1f + percentMore / 100f);
    }
}
