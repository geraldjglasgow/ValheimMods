using System;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Mutations;
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
    /// The reactions to a hit that has landed: Warding reflects a capped share of the base hit back at the attacker
    /// (see <see cref="WardingReflect"/>) and knocks it back on a melee hit, Leeching heals the attacker for a share of
    /// what it dealt, and a Reflective boss returns a share of what it took as true damage. All key off what the hit
    /// actually did, so they run in a postfix once the hit is resolved, on the victim's owner, reading their
    /// percentages from the biome rules each creature carries. The prefix only records what Warding needs from before
    /// the hit (its health, backstab stamp and stagger state), and never throws, so it can never stop a hit landing.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class DamageReactionPatch
    {
        private static void Prefix(Character __instance, out WardingReflect.Before __state)
        {
            __state = default;
            try
            {
                __state = WardingReflect.Capture(__instance);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.RPC_Damage warding capture");
            }
        }

        private static void Postfix(Character __instance, HitData hit, WardingReflect.Before __state) =>
            Guard.Run("Character.RPC_Damage reaction", () => React(__instance, hit, __state));

        private static void React(Character victim, HitData hit, WardingReflect.Before before)
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
            Warding(victim, attacker, hit, before);
            Leech(attacker, dealt);
            ReflectiveHit.Return(victim, attacker, dealt);
        }

        private static void Warding(Character victim, Character attacker, HitData hit, WardingReflect.Before before)
        {
            EliteController? controller = With(victim, Mutation.Warding);
            if (controller == null)
            {
                return;
            }
            WardingReflect.Return(controller, victim, attacker, hit, before);
            if (!hit.m_ranged)
            {
                Vector3 dir = (attacker.transform.position - victim.transform.position).normalized;
                attacker.ApplyPushback(dir,
                    Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Warding, Fields.Knockback));
            }
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
