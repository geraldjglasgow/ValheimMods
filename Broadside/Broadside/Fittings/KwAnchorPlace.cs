using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The anchor windlass's and the captain's wheel's ghosts go only on a deck (they work on the launched ship's deck), the
    /// wheel on a cabin's roof's top too (<see cref="KwCabinRoofs.OnRoof"/>; its steering gear climbs the cabin to it,
    /// <see cref="KwGearClimb"/>): off one they are refused. They may face any way; the windlass's cable is led to the bow wherever it stands. Runs after the game has placed the ghost and found
    /// it valid.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwAnchorPlace
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside anchor placement", Fit, __instance);

        private static void Fit(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            Piece? selected = ghost != null && ghost.activeSelf ? player.GetSelectedPiece() : null;
            if (selected == null || (selected.name != KwAnchor.Prefab && selected.name != KwWheel.Prefab) || player.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            Piece? under = KwTools.Aim(player)?.Piece;
            if (under != null && (Utils.GetPrefabName(under.gameObject) == KwToolPieces.PartPrefix + "deck"
                || (selected.name == KwWheel.Prefab && KwCabinRoofs.OnRoof(under, ghost!.transform.position))))
                return;
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            player.SetPlacementGhostValid(false);
        }
    }
}
