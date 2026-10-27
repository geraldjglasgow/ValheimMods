using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The owner's answer to a player asking for a Broadside ship's helm (the game's ShipControlls, on the asking player's
    /// game): granted, he takes it through <see cref="KwHelmsman.Take"/> in place of the game's own attach, so at a tiller
    /// or a captain's wheel he stays on his feet. Any other ship's helm, a refusal, or a throw: the game's own answer.
    /// </summary>
    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.RPC_RequestRespons))]
    public static class KwHelmTakePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ShipControlls __instance, bool granted)
        {
            Player? me = Player.m_localPlayer;
            KwHelmsman? helmsman = granted && me != null ? __instance.GetComponent<KwHelmsman>() : null;
            if (helmsman == null || !helmsman.Steers)
                return true;
            try
            {
                Guard.Run("Broadside helm", static (h, p) => h.Take(p), helmsman, me!);
                return false;
            }
            catch (System.Exception)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// The helmsman's feet at a tiller or a captain's wheel (<see cref="KwHelmFeet"/>): once the game has worked out the step's movement (none:
    /// the helm takes his movement keys), they are walked to his stand. Only the local player's controls come through here.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class KwHelmWalkPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            KwHelmsman? mine = KwHelmsman.Mine;
            if (mine != null && mine.Walking(__instance))
                Guard.Run("Broadside helmsman walk", static (h, p) => h.Steer(p), mine, __instance);
        }
    }

    /// <summary>
    /// The helmsman's facing at a tiller is the bow, at a captain's wheel the way the wheel faces (<see cref="KwHelmFeet.Face"/>),
    /// kept while his feet step about. Every character passes through here each physics step; anyone but the local
    /// helmsman of a Broadside ship is untouched, and should it throw, the game turns him as it would.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateRotation))]
    public static class KwHelmFacingPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Character __instance, ref float __result)
        {
            KwHelmsman? mine = KwHelmsman.Mine;
            if (mine == null || !mine.Walking(__instance))
                return true;
            try
            {
                __result = Guard.Run("Broadside helmsman facing", static (h, c) => h.Face((Player)c, Time.fixedDeltaTime), mine, __instance);
                return false;
            }
            catch (System.Exception)
            {
                return true;
            }
        }
    }
}
