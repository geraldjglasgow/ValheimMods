using System;
using EliteCreaturesReborn.Mutations;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Cloning's two blows (<see cref="CloneHits"/>), on the struck one's owner, where the game resolves every hit: a
    /// decoy's is emptied, and a hidden creature's landing on this machine's player tells the creature's owner to show
    /// it. Priority.Last, so the decoy's blow is emptied after every other prefix has scaled or added to it (its stars,
    /// another mod's bonus) and nothing survives. Never rethrows: a throw here would stop the hit from landing at all.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class CloneHitPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Character __instance, HitData hit)
        {
            try
            {
                if (hit != null && CloneHits.Struck(__instance, hit))
                {
                    CloneHits.Report(hit.GetAttacker());
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.RPC_Damage cloning");
            }
        }
    }
}
