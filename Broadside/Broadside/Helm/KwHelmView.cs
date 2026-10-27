using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The view while captaining a Broadside ship: the camera may zoom out far enough to see the whole ship and the sea
    /// round it, the game's ship zoom (<c>m_maxDistanceBoat</c>, used while the player controls a ship) stretched to twice
    /// the game's or one and a half times the hull's length, whichever is more; the game's own again once the player lets
    /// go of the helm or steers a ship of the game's. Local only.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    public static class KwHelmView
    {
        private const float Times = 2f, Lengths = 1.5f;
        private static float gameBoat = -1f;

        [HarmonyPrefix]
        private static void Prefix(GameCamera __instance) => Guard.Run("Broadside helm view", Stretch, __instance);

        private static void Stretch(GameCamera camera)
        {
            if (gameBoat < 0f)
                gameBoat = camera.m_maxDistanceBoat;
            Ship? ship = Player.m_localPlayer != null ? Player.m_localPlayer.GetControlledShip() : null;
            KwShipBody? body = ship != null ? ship.GetComponent<KwShipBody>() : null;
            float length = body != null && body.Hull != null ? body.Hull.Length : 0f;
            camera.m_maxDistanceBoat = body != null ? Mathf.Max(gameBoat * Times, length * Lengths) : gameBoat;
        }
    }
}
