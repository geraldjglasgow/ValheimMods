using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The reactions to a hit that has landed: Warding reflects a share back at the attacker and knocks it back on a
    /// melee hit, Leeching heals the attacker for a share of what it dealt, and a Reflective boss returns a share of
    /// what it took as true damage. Both key off the amount actually dealt,
    /// so they run in a postfix once the hit is resolved, on the victim's owner, reading their percentages from the
    /// biome rules each creature carries.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class DamageReactionPatch
    {
        private static void Postfix(Character __instance, HitData hit) =>
            Guard.Run("Character.RPC_Damage reaction", () => React(__instance, hit));

        private static void React(Character victim, HitData hit)
        {
            ZNetView nview = victim.GetComponent<ZNetView>();
            if (hit == null || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            float dealt = hit.GetTotalDamage();
            Character attacker = hit.GetAttacker();
            if (dealt <= 0f || attacker == null)
            {
                return;
            }
            Warding(victim, attacker, hit, dealt);
            Leech(attacker, dealt);
            ReflectiveHit.Return(victim, attacker, dealt);
        }

        private static void Warding(Character victim, Character attacker, HitData hit, float dealt)
        {
            EliteController? controller = With(victim, Mutation.Warding);
            if (controller == null)
            {
                return;
            }
            float reflect = Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Warding, Fields.Reflect);
            attacker.Damage(Reflected(victim, attacker, dealt * reflect / 100f));
            // Route the flash through THIS creature's own ZNetView, not the world-wide bus: Warding fires on every melee
            // hit, so scoping it to the clients holding the creature (the attacker included) keeps the wire quiet.
            CreatureRpc.FireFlash(controller.View, attacker.GetCenterPoint(), 2f, "reflect");
            if (!hit.m_ranged)
            {
                Vector3 dir = (attacker.transform.position - victim.transform.position).normalized;
                attacker.ApplyPushback(dir,
                    Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Warding, Fields.Knockback));
            }
        }

        private static HitData Reflected(Character victim, Character attacker, float amount)
        {
            HitData reflected = new HitData();
            reflected.m_damage.m_blunt = amount;
            reflected.m_point = attacker.transform.position;
            reflected.m_dir = (attacker.transform.position - victim.transform.position).normalized;
            reflected.m_hitType = HitData.HitType.EnemyHit;
            return reflected;
        }

        private static void Leech(Character attacker, float dealt)
        {
            EliteController? controller = With(attacker, Mutation.Leeching);
            if (controller != null)
            {
                float steal = Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Leeching, Fields.Lifesteal);
                attacker.Heal(dealt * steal / 100f, showText: false);
            }
        }

        private static EliteController? With(Character character, Mutation mutation)
        {
            EliteController? controller = character.GetComponent<EliteController>();
            return controller != null && controller.Ready && controller.Traits.Has(mutation) ? controller : null;
        }
    }
}
