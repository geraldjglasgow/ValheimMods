using EliteCreaturesReborn.Recap;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Every hit that lands on this machine's own player goes into the death recap's hit log. <c>ApplyDamage</c> is where
    /// a hit lands (a creature's swing, an arrow, a burning or poison tick, a fall, drowning, the game's other causes and
    /// the mod's own storms): the prefix notes the health before, the postfix records the hit when health was lost.
    /// Anything else is one reference check. Inside the game's hit, so a failure is reported and swallowed.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class RecapHitPatch
    {
        private static void Prefix(Character __instance, out float __state) =>
            __state = __instance == Player.m_localPlayer && !__instance.IsDead() ? __instance.GetHealth() : -1f;

        private static void Postfix(Character __instance, HitData hit, float __state)
        {
            if (__state > 0f && __instance.GetHealth() < __state)
            {
                SafeCall.Run("Character.ApplyDamage death recap", () => HitLog.Record((Player)__instance, hit));
            }
        }
    }

    /// <summary>
    /// The creature behind later burning and poison ticks: <c>RPC_Damage</c> takes a hit's fire, spirit and poison off
    /// before it lands and turns them into ticks with no attacker, so its prefix notes who carried them.
    /// </summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    public static class RecapSourcePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (hit != null && __instance == Player.m_localPlayer)
            {
                SafeCall.Run("Character.RPC_Damage death recap", () => HitLog.NoteSource(hit));
            }
        }
    }
}
