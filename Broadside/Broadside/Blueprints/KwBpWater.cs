using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A saved ship's ghost on the water, as the game's own ships are placed. The game puts a water piece's ghost a few
    /// metres over the surface it hits; this sinks the ship to a floating depth instead (its keel as deep as the launched
    /// ship floats, <see cref="KwShipTuning.Draught"/>), and refuses water too shallow for that: the sea floor under any part of the hull
    /// (its corners, the middle of each side and its centre) must lie below the keel. Runs after the game has placed the
    /// ghost and found it valid (on water, room, the workbench and the cost are the game's own checks).
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwBpWater
    {
        /// <summary>Why the ghost was refused, for the click's message; null when it was not refused here.</summary>
        public static string? Reason { get; private set; }

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside ship on water", Settle, __instance);

        private static void Settle(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            KwBpEntry? entry = ghost != null && ghost.activeSelf ? KwBpEntries.Of(player.GetSelectedPiece()) : null;
            Reason = null;
            if (entry == null || player.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            float keel = ZoneSystem.instance.m_waterLevel - KwShipTuning.Draught(entry.Hull);
            ghost!.transform.position = new Vector3(ghost.transform.position.x, keel, ghost.transform.position.z);
            if (Deep(ghost.transform, entry, keel))
                return;
            Reason = "Too shallow here: the hull would sit on the sea floor";
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            player.SetPlacementGhostValid(false);
        }

        // The sea floor under the hull's footprint all lies below the keel.
        private static bool Deep(Transform ghost, KwBpEntry entry, float keel)
        {
            KwBpHull hull = entry.Hull;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Vector3 local = new Vector3(i * hull.Beam / 2f, 0f, j * hull.Length / 2f);
                    if (ZoneSystem.instance.GetGroundHeight(ghost.TransformPoint(local)) > keel - 0.05f)
                        return false;
                }
            }
            return true;
        }
    }
}
