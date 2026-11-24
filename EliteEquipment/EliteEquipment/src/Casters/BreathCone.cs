using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Who one burst of Moder's Breath hits, on the caster's machine: every character whose body reaches into the cone from
    /// the wand's tip toward the aim (<see cref="Length"/> m long, <see cref="HalfAngle"/> degrees each side of the aim,
    /// widened by the body's radius), with nothing solid (ground, rock, a building) between the tip and the body's middle,
    /// and only the caster's enemies, as the bolt's projectile picks them (<c>Projectile.IsValidTarget</c>): never the
    /// caster, never a tame, other players only with PvP on for both, no one dodging. Each gets <see cref="BreathHit"/>
    /// (<c>Character.Damage</c>, which the game sends to the target's owner); a burst that hits anyone raises Elemental
    /// Magic and gives adrenaline once, as a bolt's hit does.
    /// </summary>
    public static class BreathCone
    {
        public const float Length = 7f;
        public const float HalfAngle = 15f;

        private static readonly List<Character> near = new List<Character>();
        private static readonly float spread = Mathf.Tan(HalfAngle * Mathf.Deg2Rad);
        private static int solid;

        public static void Burst(Player caster, ItemDrop.ItemData wand, Vector3 from, Vector3 way)
        {
            bool hitAny = false;
            near.Clear();
            Character.GetCharactersInRange(from, Length + 2f, near);
            foreach (Character target in near)
            {
                if (!Hits(caster, target) || !Inside(from, way, target) || Blocked(from, target))
                    continue;
                target.Damage(BreathHit.For(caster, wand, target.GetCenterPoint(), way));
                hitAny = true;
            }
            near.Clear();
            if (hitAny)
                Raise(caster, wand);
        }

        private static void Raise(Player caster, ItemDrop.ItemData wand)
        {
            caster.RaiseSkill(wand.m_shared.m_skillType, wand.m_shared.m_attack.m_raiseSkillAmount);
            caster.AddAdrenaline(1f);
        }

        /// <summary>The bolt's target rule (<c>Projectile.IsValidTarget</c>, not friendly), and never a tame.</summary>
        private static bool Hits(Player caster, Character target)
        {
            if (target == null || target == caster || target.IsDead() || target.IsTamed() || target.IsDodgeInvincible())
                return false;
            if (target.IsPlayer())
                return caster.IsPVPEnabled() && target.IsPVPEnabled();
            BaseAI ai = target.GetBaseAI();
            return BaseAI.IsEnemy(caster, target) || (ai != null && ai.IsAggravatable());
        }

        private static bool Inside(Vector3 from, Vector3 way, Character target)
        {
            Vector3 to = target.GetCenterPoint() - from;
            float along = Vector3.Dot(to, way);
            float radius = target.GetRadius();
            if (along < -radius || along > Length + radius)
                return false;
            float off = (to - way * along).magnitude;
            return off <= Mathf.Max(along, 0f) * spread + radius;
        }

        private static bool Blocked(Vector3 from, Character target)
        {
            if (solid == 0)
                solid = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            return Physics.Linecast(from, target.GetCenterPoint(), solid, QueryTriggerInteraction.Ignore);
        }
    }
}
