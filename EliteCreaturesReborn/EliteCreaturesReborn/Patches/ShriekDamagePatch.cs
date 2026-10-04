using System;
using EliteCreaturesReborn.Mutations;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Hands a Screecher the health one hit actually took from it (<see cref="ScreecherBehaviour.Hurt"/>), on the
    /// creature's owner, where the game resolves every hit. The prefix records the health before the hit, only for a
    /// Screecher on its deciding machine, and never throws, so it can never stop a hit landing; the postfix reads the
    /// health after, so a blocked, resisted or ignored hit counts only what got through. A failure in the shriek is
    /// reported and swallowed: the hit has landed by then and must stay landed.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class ShriekDamagePatch
    {
        private static void Prefix(Character __instance, out float __state)
        {
            __state = -1f;
            try
            {
                ScreecherBehaviour screecher = __instance.GetComponent<ScreecherBehaviour>();
                if (screecher != null && screecher.Deciding)
                {
                    __state = __instance.GetHealth();
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.RPC_Damage screecher capture");
            }
        }

        private static void Postfix(Character __instance, float __state)
        {
            if (__state >= 0f)
            {
                SafeCall.Run("Character.RPC_Damage screecher", () => React(__instance, __state));
            }
        }

        private static void React(Character victim, float before)
        {
            ScreecherBehaviour screecher = victim.GetComponent<ScreecherBehaviour>();
            if (screecher != null)
            {
                screecher.Hurt(before - Mathf.Max(0f, victim.GetHealth()));
            }
        }
    }
}
