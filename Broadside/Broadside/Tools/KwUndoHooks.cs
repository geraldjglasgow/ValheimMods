using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// A Broadside ship piece the local player places from the hammer (a hatch, a post cap, a rudder, or any fitting
    /// <see cref="KwBlueprintStation.IsShipPiece"/> counts; not the dock pieces or the table) is one action for undo
    /// (<see cref="KwUndo"/>). The tools never get here: their clicks are taken before the game places anything.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    public static class KwUndoPlaced
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, Piece piece) => Guard.Run("Broadside undo record", Placed, __instance, piece);

        private static void Placed(Player player, Piece piece)
        {
            if (player != Player.m_localPlayer || piece == null || !KwBlueprintStation.IsShipPiece(piece.name))
                return;
            Piece? made = Newest(piece.name, player.GetPlayerID());
            if (made != null)
                KwUndo.Made(player, made.gameObject, Localization.instance.Localize(made.m_name), false);
        }

        // The piece the game just placed: the newest in its list of every piece (each adds itself there as it wakes),
        // checked to be of that prefab and this player's.
        private static Piece? Newest(string prefab, long creator)
        {
            List<Piece> all = Piece.s_allPieces;
            Piece? last = all.Count > 0 ? all[all.Count - 1] : null;
            return last != null && Utils.GetPrefabName(last.gameObject) == prefab && last.GetCreator() == creator ? last : null;
        }
    }

    /// <summary>The undo keys' hidden object (<see cref="KwUndoKeys"/>) is made the first time the player takes out a build tool.</summary>
    [HarmonyPatch(typeof(Player), "SetPlaceMode")]
    public static class KwUndoHammer
    {
        [HarmonyPostfix]
        private static void Postfix(PieceTable buildPieces)
        {
            if (buildPieces != null)
                KwUndoKeys.Ensure();
        }
    }
}
