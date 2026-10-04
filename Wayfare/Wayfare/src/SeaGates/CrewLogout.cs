using HarmonyLib;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>A player who logs out mid-jump, or loses the server, is held over the water where the ship will be. The
    /// game saves the logout point from the player's position (<c>PlayerProfile.SaveLogoutPoint</c>, on logout, on a
    /// disconnect and on every save) and spawns them there next time (<c>Game.FindSpawnPoint</c>, which lifts a point
    /// below the ground onto it), so while a hold lasts the logout point is the dry spot beside the destination gate
    /// instead. A later save outside a jump writes the player's own position again. Not gated on the settings: it only
    /// acts during a hold.</summary>
    [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SaveLogoutPoint))]
    public static class CrewLogoutPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerProfile __instance)
        {
            if (__instance == null || ZNet.instance == null || !CrewHold.TryLogoutPoint(out Vector3 point))
                return;
            __instance.SetLogoutPoint(point);
        }
    }
}
