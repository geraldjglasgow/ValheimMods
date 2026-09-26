using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Reflective: a share of the damage a hit actually dealt to the boss is dealt back to whoever landed it. Called from
    /// the damage-reaction postfix on the boss's owner, which only ever sees direct hits - the game applies burn and
    /// poison ticks straight to health, never through that path - so a tick is never returned. The returned hit is true
    /// damage (the game's untyped damage, which armour and resistances do not touch) and cannot be blocked or dodged, so
    /// no gear turns the aspect into a non-event. It carries no attacker, so nothing reflects it back again.
    /// </summary>
    internal static class ReflectiveHit
    {
        public static void Return(Character boss, Character attacker, float dealt)
        {
            EliteController controller = boss.GetComponent<EliteController>();
            if (controller == null || !controller.Ready || controller.Traits.Aspect != Aspect.Reflective || attacker == boss)
            {
                return;
            }
            float amount = dealt * AspectMath.Power(Aspect.Reflective, Fields.Reflect) / 100f;
            if (amount <= 0f)
            {
                return;
            }
            attacker.Damage(TrueHit(boss, attacker, amount));
            CreatureRpc.FireFlash(controller.View, attacker.GetCenterPoint(), 1.5f, "reflect");
        }

        private static HitData TrueHit(Character boss, Character attacker, float amount)
        {
            HitData hit = new HitData();
            hit.m_damage.m_damage = amount;
            hit.m_point = attacker.GetCenterPoint();
            hit.m_dir = (attacker.transform.position - boss.transform.position).normalized;
            hit.m_hitType = HitData.HitType.EnemyHit;
            hit.m_blockable = false;
            hit.m_dodgeable = false;
            hit.m_pushForce = 0f;
            return hit;
        }
    }
}
