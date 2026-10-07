using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.BearClaws
{
    /// <summary>
    /// A bear claw punch becomes a flurry (<see cref="BearClawFlurry"/>). While one runs, the player's own attack starts
    /// wait (a click stays queued, as the game queues clicks during an attack); only the flurry's next swipe goes through.
    /// The game starts attacks on the attacker's own machine only. A failure is reported and the punch plays as the game's.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    public static class BearClawStartPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (BearClawFlurry.StartingSwipe || !(__instance is Player player) || !Flurrying(player))
            {
                return true;
            }
            __result = false;
            return false;
        }

        private static void Postfix(Humanoid __instance, bool __result)
        {
            if (__result && !BearClawFlurry.StartingSwipe && __instance is Player player)
            {
                SafeCall.Run("Humanoid.StartAttack bear claws", static p => Begin(p), player);
            }
        }

        private static bool Flurrying(Player player)
        {
            BearClawFlurry? flurry = player.GetComponent<BearClawFlurry>();
            return flurry != null && flurry.Active;
        }

        private static void Begin(Player player)
        {
            if (BearClawSwipe.IsClawPunch(player))
            {
                BearClawFlurry.Begin(player);
            }
        }
    }

    /// <summary>
    /// A flurry's second and third swipes wear no durability and their misses add no adrenaline: the punch's first swipe
    /// already did. Restored after the hit, whatever happens.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
    public static class BearClawTriggerPatch
    {
        private static void Prefix(Attack __instance, out (float durability, float missAdrenaline)? __state)
        {
            __state = null;
            if (__instance.m_character is Player player && player.TryGetComponent(out BearClawFlurry flurry)
                && flurry.IsExtra(__instance))
            {
                __state = (__instance.m_weapon.m_durability, player.m_attackMissAdrenaline);
                player.m_attackMissAdrenaline = 0f;
            }
        }

        private static void Finalizer(Attack __instance, (float durability, float missAdrenaline)? __state)
        {
            if (__state is (float durability, float missAdrenaline) && __instance.m_character is Player player)
            {
                __instance.m_weapon.m_durability = durability;
                player.m_attackMissAdrenaline = missAdrenaline;
            }
        }
    }

    /// <summary>
    /// A flurry steps forward <see cref="BearClawFlurry.Step"/> as far as its punches would: the animation's root motion,
    /// gathered on the owner, is cut while it swipes. Runs for every character's animator move, so one reference check.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.AddRootMotion))]
    public static class BearClawStepPatch
    {
        private static void Prefix(Character __instance, ref Vector3 vel)
        {
            if (ReferenceEquals(__instance, BearClawFlurry.Swiper))
            {
                vel *= BearClawFlurry.Step;
            }
        }
    }

    /// <summary>A flurry's hits freeze the swing a third as long, so three hits hold it as long as the punch's one.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.FreezeFrame))]
    public static class BearClawFreezePatch
    {
        private static void Prefix(Character __instance, ref float duration)
        {
            if (__instance is Player player && player.TryGetComponent(out BearClawFlurry flurry) && flurry.Active)
            {
                duration /= BearClawSwipe.Swipes;
            }
        }
    }
}
