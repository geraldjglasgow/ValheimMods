using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Binding's hold reaches the roll too: while roots hold a player (<see cref="TrailStatus.Rooted"/>), a roll they
    /// ask for is dropped before the game starts it, on their own machine where the game runs it. The hold's status
    /// already takes their walking and their jump; this is the one way out it does not cover. A roll already under way
    /// when the roots take hold finishes.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdateDodge")]
    public static class RootDodgePatch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance.m_queuedDodgeTimer > 0f && TrailStatus.Rooted(__instance))
            {
                __instance.m_queuedDodgeTimer = 0f;
            }
        }
    }
}
