using System;
using EliteCreaturesReborn.Tally;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The boss damage board's tally, on the boss's owner, where the game applies every hit. The health before the hit is
    /// read in the prefix and the loss credited to the attacking player in the postfix, so the tally counts what the
    /// boss actually lost: after its resistances, and never the overkill below zero. Only players are credited; a
    /// Phantom copy is a decoy and counts nothing. The prefix never throws, so it can never stop a hit landing.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class BossDamagePatch
    {
        private static void Prefix(Character __instance, out float __state)
        {
            __state = 0f;
            try
            {
                __state = __instance.IsBoss() ? __instance.GetHealth() : 0f;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.ApplyDamage boss tally");
            }
        }

        private static void Postfix(Character __instance, HitData hit, float __state) =>
            SafeCall.Run("Character.ApplyDamage boss tally", () => Credit(__instance, hit, __state));

        private static void Credit(Character boss, HitData hit, float before)
        {
            ZNetView nview = boss.m_nview;
            if (before <= 0f || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            float loss = before - Mathf.Max(0f, boss.GetHealth());
            ZDO zdo = nview.GetZDO();
            if (loss > 0f && hit.GetAttacker() is Player player && AspectStore.GetPhantomOf(zdo) == ZDOID.None)
            {
                DamageTally.Add(zdo, player, loss);
            }
        }
    }

    /// <summary>
    /// The boss damage board's announcement, on the boss's owner as it dies, while its ZDO still holds the tally (the
    /// game resets the ZDO by the end of <c>OnDeath</c>). Never throws: nothing here may stop a boss dying.
    /// </summary>
    [HarmonyPatch(typeof(Character), "OnDeath")]
    public static class BossBoardDeathPatch
    {
        private static void Prefix(Character __instance) =>
            SafeCall.Run("Character.OnDeath boss board", () => Announce(__instance));

        private static void Announce(Character victim)
        {
            ZNetView nview = victim.m_nview;
            if (victim.IsBoss() && nview != null && nview.IsValid() && nview.IsOwner())
            {
                BossBoardRpc.Announce(victim, nview.GetZDO());
            }
        }
    }

    /// <summary>World start, on every machine: the board's message is registered before any boss can fall.</summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    public static class BossBoardStartPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start boss board", BossBoardRpc.EnsureRegistered);
    }
}
