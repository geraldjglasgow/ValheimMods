using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's ghost set square in a gunport (the user, 2026-11-02: "When placing a cannon allow holding shift if
    /// it's near a porthole and have the cannon preview perfectly centered in the porthole facing directly out"). While the
    /// local player places a ship cannon on a launched ship (<see cref="KwShipRiderAim.Ship"/>) and holds the game's
    /// alternate placement (<see cref="Held"/>: AltPlace, Left Shift by default, or a gamepad's, by the game's own test, so
    /// a rebound key works), and one of that ship's openings but its doorways is near the aim on its deck
    /// (<see cref="KwGunports.Nearest"/>: its middle within <see cref="KwGunports.Reach"/> across the deck), the placement
    /// point is moved to where a cannon stands run out behind it (<see cref="KwGunports.Stand"/>: on the port's middle line,
    /// on the deck under it), so the game's own checks (wards, no-build places, players in the way) are made there; once the
    /// game has placed the ghost it is set exactly there (<see cref="KwArmPortGhost"/>; the ghost keeps only its placing box,
    /// <see cref="KwGhostPlace"/>, so nothing else of it is moved), turned to face straight out through the port: square to
    /// the wall at the port's middle, level in the ship's frame. The game then places it as any piece
    /// (<c>TryPlacePiece</c>), and it rides the ship (<see cref="KwShipRiderPlaced"/>). Without the key, or with no port
    /// near, placing is as before. Local to the placing player; the piece placed replicates as any other.
    /// </summary>
    [HarmonyPatch(typeof(Player), "PieceRayTest")]
    public static class KwArmPortSnap
    {
        private static bool pending;
        private static Vector3 at;
        private static Quaternion turn;

        // After the ship's own placement ray (KwShipRiderAim), which finds the launched ship aimed at this frame.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void Postfix(Player __instance, bool __result, ref Vector3 point)
        {
            if (__instance != Player.m_localPlayer)
                return;
            pending = false;
            if (!__result)
                return;
            try
            {
                (Vector3 At, Quaternion Turn)? snap = Snap(__instance, point);
                if (snap != null)
                    (point, at, turn, pending) = (snap.Value.At, snap.Value.At, snap.Value.Turn, true);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside cannon gunport placement");
            }
        }

        /// <summary>After the game has placed the ghost, in the same call: set exactly behind the port, facing out through it.</summary>
        public static void Place(Player player)
        {
            if (!pending || player != Player.m_localPlayer)
                return;
            pending = false;
            GameObject? ghost = player.m_placementGhost;
            if (ghost != null && ghost.activeSelf)
                ghost.transform.SetPositionAndRotation(at, turn);
        }

        /// <summary>The game's own test for its alternate placement (as <c>UpdatePlacementGhost</c> makes it), so a rebound key, a gamepad's button and its newer scheme's toggle all work.</summary>
        public static bool Held(Player player) => ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()
            ? player.AlternativePlacementActive
            : ZInput.GetButton("AltPlace") || (ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate"));

        // Where the ghost goes, and its turn, with the key held near a port of the launched ship aimed at; null for none.
        private static (Vector3 At, Quaternion Turn)? Snap(Player player, Vector3 point)
        {
            KwShipRiders? ship = KwShipRiderAim.Ship;
            Piece? selected = ship != null && Held(player) ? player.GetSelectedPiece() : null;
            KwShipSpec? spec = selected != null ? KwArmory.OfPrefab(selected.name)?.Ship : null;
            KwShipOpenings? openings = spec != null ? ship!.GetComponent<KwShipOpenings>() : null;
            KwShipBody? body = openings != null ? ship!.GetComponent<KwShipBody>() : null;
            if (openings == null || openings.Plan == null || body == null || body.Blueprint == null)
                return null;
            Transform frame = ship!.transform;
            Transform? ghost = player.m_placementGhost != null ? player.m_placementGhost.transform : null;
            (KwGunport Port, float Deck)? near = KwGunports.Nearest(KwGunports.Of(openings.Plan, body.Blueprint), frame.InverseTransformPoint(point), ship, spec!, ghost);
            if (near == null)
                return null;
            Vector3 stand = KwGunports.Stand(near.Value.Port, spec!, near.Value.Deck);
            return (frame.TransformPoint(stand), frame.rotation * Quaternion.LookRotation(near.Value.Port.Way, Vector3.up));
        }
    }

    /// <summary>A ship cannon's ghost set behind the gunport it was snapped to (<see cref="KwArmPortSnap"/>), after the game has placed it.</summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwArmPortGhost
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside cannon gunport placement", KwArmPortSnap.Place, __instance);
    }
}
