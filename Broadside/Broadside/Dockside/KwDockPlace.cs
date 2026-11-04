using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's ghost of a dock lantern or a mooring post on a dock (DOCKS.md, "Dock lanterns" and "Mooring posts on a
    /// dock"): the dock lantern stands only beside a dock's edge, snapped there, square to it and pointing in, its post shown
    /// at the length it would have in the dock's wood, the hammer showing what it would cost (<see cref="KwDockLanternClick.Hover"/>);
    /// a mooring post or the lantern pole aimed on a dock near a side snaps over the nearest piling station
    /// (<see cref="KwDockMooring.Hover"/>).
    /// Runs after the game has placed the ghost; any other piece is left alone at once. The game places the piece where the
    /// ghost then stands (its click asks for the ghost again first).
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwDockPlace
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside dock placement", Fit, __instance);

        private static void Fit(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            Piece? selected = ghost != null && ghost.activeSelf ? player.GetSelectedPiece() : null;
            if (selected != null && selected.name == KwDockLanternPiece.Prefab)
            {
                KwDockLanternClick.Hover(player, ghost!, selected);
                return;
            }
            // any other piece in hand: the dock lantern back to its resting cost (once)
            KwDockLanternClick.Rest();
            if (selected != null && KwDockMooring.OnPiling(selected.name))
                KwDockMooring.Hover(ghost!, selected.name);
        }
    }
}
