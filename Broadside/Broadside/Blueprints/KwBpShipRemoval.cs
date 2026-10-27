using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Removing a whole blueprint ship with the hammer: Alt held while removing any piece built from a blueprint. The game
    /// removes the piece aimed at as usual (its own checks: ward, station, can be removed); only when it did, every other
    /// loaded piece within 150 m carrying exactly the same ship id (<see cref="KwBlueprintStation.ShipKey"/>, one per
    /// build) goes too, through the game's removal (<see cref="KwBpJobs.Remove"/>). A piece without a ship id, or with
    /// another, is never touched.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    public static class KwBpShipRemoval
    {
        private const float Reach = 150f;
        private static string ship = "";
        private static Piece? aimed;
        private static Vector3 aimedAt;

        [HarmonyPrefix]
        private static void Prefix(Player __instance) => Guard.Run("Broadside ship removal", Remember, __instance);

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __result) => Guard.Run("Broadside ship removal", Follow, __instance, __result);

        /// <summary>
        /// The ship id a loaded piece carries, or "". Never a launched ship's: its key of the same name holds its packed
        /// blueprint, the same for every ship launched from one design, so Alt would take the others with it.
        /// </summary>
        public static string ShipOf(Piece? piece)
        {
            ZNetView? view = piece != null ? piece.m_nview : null;
            return view != null && view.IsValid() && piece!.GetComponent<KwShipBody>() == null ? view.GetZDO().GetString(KwBlueprintStation.ShipHash) : "";
        }

        private static void Remember(Player player)
        {
            ship = "";
            aimed = null;
            if (player != Player.m_localPlayer || !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                return;
            Piece? piece = Aim(player);
            string id = ShipOf(piece);
            if (piece == null || id.Length == 0)
                return;
            ship = id;
            aimed = piece;
            aimedAt = piece.transform.position;
        }

        private static void Follow(Player player, bool removed)
        {
            string id = ship;
            ship = "";
            if (id.Length == 0 || !removed)
                return;
            List<Piece> near = new List<Piece>(), rest = new List<Piece>();
            Piece.GetAllPiecesInRadius(aimedAt, Reach, near);
            foreach (Piece piece in near)
            {
                if (piece != aimed && ShipOf(piece) == id)
                    rest.Add(piece);
            }
            aimed = null;
            if (rest.Count > 0)
                KwBpJobs.Remove(player, rest, "Whole ship removed");
        }

        // The piece the game's removal aims at: the same ray, mask and reach.
        private static Piece? Aim(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, player.m_removeRayMask)
                || Vector3.Distance(hit.point, player.m_eye.position) >= player.m_maxPlaceDistance)
                return null;
            return hit.collider.GetComponentInParent<Piece>();
        }
    }
}
