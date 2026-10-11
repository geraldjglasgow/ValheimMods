using System;
using System.Collections.Generic;
using System.Reflection;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The span of a boss's blow on its owner, for Brutal (<see cref="BrutalBlow"/>): hooked on the two methods that deal a
    /// blow - the melee sweep and the area burst, which the game calls from <c>Attack.OnAttackTrigger</c> at the impact
    /// frame, only on the attacker's owner and only while the attack is still live - and every hit of the blow is sent
    /// from inside them. The prefix opens the marking for a Brutal boss's heavy blow; the finalizer closes it whatever
    /// happens, so no hit outside the blow is ever marked. Projectile, taunt and breath attacks never reach either method.
    /// Any other attacker is one bool check; a failure opening it is reported and the blow goes ahead unmarked.
    /// </summary>
    [HarmonyPatch]
    public static class BrutalBlowPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Attack), "DoMeleeAttack");
            yield return AccessTools.Method(typeof(Attack), "DoAreaAttack");
        }

        private static void Prefix(Attack __instance, Humanoid ___m_character, out bool __state)
        {
            __state = false;
            if (___m_character == null || !AspectBearers.Carries(___m_character))
            {
                return; // every creature's and player's blows come through here; only a boss (or an aspect bearer) can be Brutal
            }
            bool opened = false;
            SafeCall.Run("Attack begins (Brutal)", () => opened = BrutalBlow.Begin(__instance, ___m_character));
            __state = opened;
        }

        // Only the blow that opened the marking closes it: a blow dealt from inside it (a block-charged shield's burst,
        // when the struck player is on this machine) leaves it open for the rest of the boss's hits.
        private static Exception? Finalizer(Exception? __exception, bool __state)
        {
            if (__state)
            {
                BrutalBlow.End();
            }
            return __exception;
        }
    }

    /// <summary>
    /// A hit leaving for its target's owner (<c>Character.Damage</c>, which sends it on through the game's damage message):
    /// while a Brutal heavy blow is being dealt here, a hit of it bound for a player is marked (<see cref="BrutalBlow.Stamp"/>),
    /// so the mark travels with the hit to that player's client. Any other hit is a single bool check; a failure is
    /// reported and the hit goes unmarked.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class BrutalMarkPatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (!BrutalBlow.Live || hit == null)
            {
                return;
            }
            SafeCall.Run("Character.Damage brutal", () => BrutalBlow.Stamp(__instance, hit));
        }
    }
}
