using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The placement ray on a launched ship. The game takes no placement hit on anything with a body of its own
    /// (<c>Player.PieceRayTest</c> drops a hit whose collider has an attached rigidbody: a ship's deck among them), so no
    /// piece could be built aboard a ship. Here, for the local player and a piece that may ride a ship or is refused there
    /// (<see cref="KwShipRiderRules.Aimed"/>), a hit on a launched Broadside ship (its hull, deck, cabins, or a piece already
    /// riding it) is taken as the game takes any other, with the game's own ray, mask and reach, and the ghost stands on
    /// it, where the deck is drawn (<see cref="KwShipRiders.Shown"/>: the hit carried from the ship's physics pose to its
    /// interpolated one, so the ghost, its snapping to a rider's snap points and the piece placed all go by the ship as
    /// the player sees it); the game's other checks (wards, no-build places, players in the way, clipping) run as usual.
    /// The ship hit is kept for the frame (<see cref="Ship"/>) for the placement rules (<see cref="KwShipRiderGhost"/>)
    /// and the piece placed (<see cref="KwShipRiderPlaced"/>). The game's own ships are left as they are.
    /// </summary>
    [HarmonyPatch(typeof(Player), "PieceRayTest")]
    public static class KwShipRiderAim
    {
        private static int frame = -1;
        private static KwShipRiders? ship;
        private static bool onDeck;
        private static Vector3 hitAt;

        /// <summary>The ship the local player's placement ray hit this frame, or null.</summary>
        public static KwShipRiders? Ship => frame == Time.frameCount && ship != null ? ship : null;

        /// <summary>Whether that hit was the ship's own hull or deck (not a piece riding it).</summary>
        public static bool OnDeck => Ship != null && onDeck;

        /// <summary>Where the ray hit the ship this frame.</summary>
        public static Vector3 HitAt => hitAt;

        [HarmonyPostfix]
        private static void Postfix(Player __instance, ref bool __result, ref Vector3 point, ref Vector3 normal, ref Piece piece,
            ref Heightmap heightmap, ref Collider waterSurface, bool water)
        {
            if (__instance != Player.m_localPlayer)
                return;
            ship = null;
            if (__result)
                return;
            try
            {
                __result = Aboard(__instance, water, ref point, ref normal, ref piece);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside building on a ship");
                return;
            }
            if (__result)
                (heightmap, waterSurface) = (null!, null!);
        }

        // The game's own ray once more: a hit on a launched ship within the game's reach is taken.
        private static bool Aboard(Player player, bool water, ref Vector3 point, ref Vector3 normal, ref Piece piece)
        {
            Piece? building = player.m_placementGhost != null ? player.m_placementGhost.GetComponent<Piece>() : null;
            if (building == null || GameCamera.instance == null || KwShipRiders.Loaded.Count == 0 || !KwShipRiderRules.Aimed(building))
                return false;
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, water ? player.m_placeWaterRayMask : player.m_placeRayMask))
                return false;
            Rigidbody? body = hit.collider != null ? hit.collider.attachedRigidbody : null;
            KwShipRiders? on = body != null ? body.GetComponent<KwShipRiders>() : null;
            if (on == null || Vector3.Distance(player.m_eye.position, hit.point) >= player.m_maxPlaceDistance + building.m_extraPlacementDistance)
                return false;
            Piece? under = hit.collider!.GetComponentInParent<Piece>();
            (Vector3 at, Vector3 up) = on.Shown(hit.point, hit.normal);
            (ship, frame, hitAt) = (on, Time.frameCount, at);
            onDeck = under == null || under.GetComponent<KwShipRiders>() != null;
            (point, normal, piece) = (at, up, onDeck ? null! : under!);
            return true;
        }
    }

    /// <summary>
    /// The rules for a ghost standing on a launched ship (after the game's own checks): only Broadside's ship cannons are
    /// built there (<see cref="KwShipRiderRules"/>; any other piece's ghost is refused, and placing it says why,
    /// <see cref="KwShipRiderRefused"/>), a piece the game keeps off wood floors (a fire) is kept off the deck too, and
    /// nothing is built aboard a ship whose owner has not yet given it its id (<see cref="KwShipRiders.IdKey"/>: a moment
    /// after it comes into the scene), since its riders could not find it.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwShipRiderGhost
    {
        /// <summary>Why the local player's ghost was refused on a ship this frame, for the message as he tries to place it; null when it was not, or no words.</summary>
        public static string? Refusal { get; private set; }

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            Refusal = null;
            KwShipRiders? ship = KwShipRiderAim.Ship;
            if (ship == null || __instance.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            Piece? building = __instance.m_placementGhost != null ? __instance.m_placementGhost.GetComponent<Piece>() : null;
            string? why = Why(building, ship);
            if (why == null)
                return;
            Refusal = why.Length > 0 ? why : null;
            __instance.m_placementStatus = Player.PlacementStatus.Invalid;
            __instance.SetPlacementGhostValid(false);
        }

        // Why a ghost is refused aboard: a piece that is not a ship cannon (said), the ship without its id or a piece kept
        // off wood (silently, ""); null when it may be built.
        private static string? Why(Piece? building, KwShipRiders ship)
        {
            if (building != null && KwShipRiderRules.Refused(building))
                return KwShipRiderRules.OnlyCannons;
            return building == null || ship.Id == 0L || building.m_notOnWood && KwShipRiderAim.OnDeck ? "" : null;
        }
    }

    /// <summary>A piece refused on a launched ship says why as the player tries to place it (after the game's own "invalid placement").</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwShipRiderRefused
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __result)
        {
            if (!__result && KwShipRiderGhost.Refusal != null && __instance == Player.m_localPlayer)
                __instance.Message(MessageHud.MessageType.Center, KwShipRiderGhost.Refusal);
        }
    }

    /// <summary>
    /// The piece the local player places on a launched ship rides it (<see cref="KwShipRider.Place"/>): the newest piece of
    /// that prefab and this player, placed this frame with the ghost on the ship, its pose in the ship's frame from the
    /// ship as drawn now (as the ghost stood on it, <see cref="KwShipRiders.Shown"/>), so a ship moving under the ghost
    /// leaves the piece where the ghost stood. A ghost moved off the ship meanwhile (snapped to something more than a few
    /// metres away) places an ordinary piece.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    public static class KwShipRiderPlaced
    {
        private const float Reach = 5f;

        [HarmonyPostfix]
        private static void Postfix(Player __instance, Piece piece)
        {
            if (KwShipRiderAim.Ship == null)
                return;
            try
            {
                Placed(__instance, piece);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside building on a ship");
            }
        }

        private static void Placed(Player player, Piece piece)
        {
            KwShipRiders? ship = KwShipRiderAim.Ship;
            if (ship == null || player != Player.m_localPlayer || piece == null || !KwShipRiderRules.Rides(piece))
                return;
            Piece? made = Newest(piece.name, player.GetPlayerID());
            if (made != null && (made.transform.position - KwShipRiderAim.HitAt).sqrMagnitude < Reach * Reach)
                KwShipRider.Place(made.gameObject, ship);
        }

        // The piece the game just placed: the newest in its list of every piece, of that prefab and this player's.
        private static Piece? Newest(string prefab, long creator)
        {
            List<Piece> all = Piece.s_allPieces;
            Piece? last = all.Count > 0 ? all[all.Count - 1] : null;
            return last != null && Utils.GetPrefabName(last.gameObject) == prefab && last.GetCreator() == creator ? last : null;
        }
    }
}
