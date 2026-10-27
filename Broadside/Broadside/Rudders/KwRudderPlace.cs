using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rudder (any of the seven) goes only on a stern post: its ghost moves onto the free "Rudder" point nearest it (the
    /// foot of a post's outer face, on the keel's centre line) within <see cref="Reach"/>, turned to face along the keel
    /// toward the ship, so it hangs square and centred on the post whatever the player's rotation. With no free point
    /// near, it is refused; a point with any rudder already on it is not free. The ghost is built to the post it would hang
    /// on (<see cref="KwRudderFit"/>, its blade dropped toward the keel bottom on a high stern) and linked to the helm the stern
    /// allows (<see cref="KwRudderLayFind"/>: a tiller, or a yoke with lines or rods to a deck tiller on the deck), so it
    /// shows the size it will be and how it will be steered. Runs after the game has placed the ghost and found it valid.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwRudderPlace
    {
        private const float Reach = 2.5f, Search = 16f, Taken = 0.25f;
        private static readonly List<Piece> Near = new List<Piece>();
        private static GameObject? fittedGhost;
        private static Transform? fittedPoint;

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside rudder placement", Fit, __instance);

        private static void Fit(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            Piece? selected = ghost != null && ghost.activeSelf ? player.GetSelectedPiece() : null;
            if (selected == null || !KwRudder.Is(selected.name) || player.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            Transform? point = Point(ghost!, out Piece? keel);
            if (point == null || keel == null)
            {
                player.m_placementStatus = Player.PlacementStatus.Invalid;
                player.SetPlacementGhostValid(false);
                return;
            }
            Vector3 along = keel.transform.forward;
            along.y = 0f;
            if (Vector3.Dot(KwShapeSites.Middle(keel) - point.position, along) < 0f)
                along = -along;
            ghost!.transform.SetPositionAndRotation(point.position, Quaternion.LookRotation(along.normalized));
            Size(ghost, keel, point);
        }

        // The ghost built again to the post it is on, its blade hanging as far as it will and reaching up to the ship's
        // waterline, linked to its helm, only when the ghost or its rudder point changes (the drop looks along the keel line
        // for the ship's keel bottom, the waterline at the ship on the dock, the lay at the stern's pieces).
        private static void Size(GameObject ghost, Piece keel, Transform point)
        {
            if (ghost == fittedGhost && point == fittedPoint)
                return;
            KwRudderFit fit = (KwRudderFit.Of(keel, point.position) ?? KwRudderFit.Standard).WithWater(KwRudderWater.OnDock(point.position));
            KwRudderTier? tier = KwRudderTiers.Of(ghost);
            KwRudder.Refit(ghost, fit, tier != null ? KwRudderLayFind.Find(ghost.transform, tier, fit) : KwRudderLay.Plain);
            fittedGhost = ghost;
            fittedPoint = point;
        }

        // The free rudder point on a keel nearest the ghost, within reach.
        private static Transform? Point(GameObject ghost, out Piece? keel)
        {
            Near.Clear();
            Piece.GetAllPiecesInRadius(ghost.transform.position, Search, Near);
            Transform? best = null;
            float bestSq = Reach * Reach;
            keel = null;
            foreach (Piece piece in Near)
            {
                if (!KwKeel.IsKeel(piece))
                    continue;
                foreach (Transform child in piece.transform)
                {
                    float sq = (child.position - ghost.transform.position).sqrMagnitude;
                    if (child.name != "Rudder" || !child.CompareTag("snappoint") || sq >= bestSq || Occupied(child.position, ghost))
                        continue;
                    best = child;
                    bestSq = sq;
                    keel = piece;
                }
            }
            return best;
        }

        private static bool Occupied(Vector3 point, GameObject ghost) => Near.Exists(p =>
            p.gameObject != ghost && KwRudder.Is(Utils.GetPrefabName(p.gameObject)) && (p.transform.position - point).sqrMagnitude < Taken * Taken);
    }
}
