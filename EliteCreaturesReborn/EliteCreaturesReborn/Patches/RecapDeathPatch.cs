using EliteCreaturesReborn.Recap;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// This machine's own player dying starts a death recap (<see cref="RecapStore.OnDeath"/>): the hits so far are taken
    /// from the log and the video keeps recording a moment longer. Only the owner runs <c>Player.OnDeath</c> through; the
    /// prefix runs before it and must never stop a death, so a failure is reported and swallowed.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class RecapDeathPatch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance == Player.m_localPlayer && !__instance.IsDead())
            {
                SafeCall.Run("Player.OnDeath death recap", () => RecapStore.OnDeath(__instance));
            }
        }
    }
}
