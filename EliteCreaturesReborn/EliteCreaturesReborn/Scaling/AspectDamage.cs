using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// The aspects that change a hit as it lands, applied from the damage patch on the victim's owner after the star
    /// scaling, so they multiply the starred hit. Outgoing: Twin's and a Phantom copy's cut to the whole hit, Enraged's
    /// boost to its physical parts, Elementalist's to its elemental ones (poison and fire included, before the game
    /// turns them into their ticking effects). Incoming: Shielded's cut to hits from bows and crossbows.
    /// </summary>
    public static class AspectDamage
    {
        public static void Outgoing(EliteController attacker, HitData hit)
        {
            CreatureTraits traits = attacker.Traits;
            float factor = AspectMath.DamageFactor(traits);
            if (!Mathf.Approximately(factor, 1f))
            {
                hit.ApplyModifier(factor);
            }
            if (traits.Aspect == Aspect.Enraged && !traits.PhantomCopy)
            {
                Physical(hit, AspectMath.Boost(AspectMath.Power(Aspect.Enraged, Fields.PhysicalBonus)));
            }
            if (traits.Aspect == Aspect.Elementalist && !traits.PhantomCopy)
            {
                Elemental(hit, AspectMath.Boost(AspectMath.Power(Aspect.Elementalist, Fields.ElementalBonus)));
            }
        }

        public static void Incoming(EliteController victim, HitData hit)
        {
            if (victim.Traits.Aspect == Aspect.Shielded && IsArrow(hit))
            {
                hit.ApplyModifier(AspectMath.Cut(AspectMath.Power(Aspect.Shielded, Fields.ArrowReduction)));
            }
        }

        /// <summary>"Arrows" means everything a bow or a crossbow fires; thrown weapons and staffs are not arrows.</summary>
        private static bool IsArrow(HitData hit) =>
            hit.m_skill == Skills.SkillType.Bows || hit.m_skill == Skills.SkillType.Crossbows;

        private static void Physical(HitData hit, float factor)
        {
            hit.m_damage.m_blunt *= factor;
            hit.m_damage.m_slash *= factor;
            hit.m_damage.m_pierce *= factor;
        }

        private static void Elemental(HitData hit, float factor)
        {
            hit.m_damage.m_fire *= factor;
            hit.m_damage.m_frost *= factor;
            hit.m_damage.m_lightning *= factor;
            hit.m_damage.m_poison *= factor;
            hit.m_damage.m_spirit *= factor;
        }
    }
}
