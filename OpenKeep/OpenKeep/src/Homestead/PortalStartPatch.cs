using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.TeleportTo(Vector3, Quaternion, bool)</c> postfix: a long jump of the local player has started (the
    /// game returns true only on the owner's client once the jump is set up), so <see cref="PortalScreen"/> decides
    /// now whether it needs the teleport screen.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    public static class PortalStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Vector3 pos, bool distantTeleport, bool __result)
        {
            if (__result && distantTeleport && __instance == Player.m_localPlayer)
                PortalScreen.Started(pos);
        }
    }
}
