using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hatch's ghost goes only on a deck, square to it: turned to the deck's heading plus the quarter turn nearest the
    /// player's own rotation, so its opening lines up with the deck's boards and its hinge can face any way. Off a deck it
    /// is refused. Runs after the game has placed the ghost and found it valid.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwHatchPlace
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside hatch placement", Fit, __instance);

        private static void Fit(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            Piece? selected = ghost != null && ghost.activeSelf ? player.GetSelectedPiece() : null;
            if (selected == null || !selected.name.StartsWith(KwHatch.Prefix) || player.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            Piece? under = KwTools.Aim(player)?.Piece;
            if (under == null || Utils.GetPrefabName(under.gameObject) != KwToolPieces.PartPrefix + "deck")
            {
                player.m_placementStatus = Player.PlacementStatus.Invalid;
                player.SetPlacementGhostValid(false);
                return;
            }
            float heading = under.transform.eulerAngles.y;
            float turns = Mathf.Round(Mathf.DeltaAngle(heading, ghost!.transform.eulerAngles.y) / 90f);
            ghost.transform.rotation = Quaternion.Euler(0f, heading + turns * 90f, 0f);
        }
    }
}
