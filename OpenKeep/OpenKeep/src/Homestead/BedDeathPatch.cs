using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.OnDeath</c> postfix on the dying player's own client (the game runs the method only there), after the
    /// game recorded the death point and asked for its respawn in 10 s. With Nearest Bed Respawn the nearest owned bed
    /// becomes the spawn point; here and not at the respawn, because the player and its custom data exist only until
    /// the game's respawn request destroys them. Then either the map opens for a choice of bed (<see cref="BedChoice"/>),
    /// or Quick Respawn replaces the game's 10 s with the wait for that bed.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class BedDeathPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            Vector3 point = __instance.transform.position;
            BedWait.Died(point);
            List<Vector3> beds = BedSettings.NearestBedRespawn.Value ? BedRespawn.Choose(point) : new List<Vector3>();
            if (!BedChoice.TryOpen(__instance, beds, point) && BedSettings.QuickRespawn.Value)
                BedWait.Schedule();
        }
    }
}
