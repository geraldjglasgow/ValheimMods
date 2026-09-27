using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// Brings the local player's build menu up to date after the tables changed mid-game. The game selects a piece by
    /// its index in the menu, so a removed or inserted entry would silently shift the selection to a neighbour: the
    /// player keeps the same piece when it is still listed, and gets the first one when it is gone (never nothing).
    /// </summary>
    public static class PlayerRefresh
    {
        /// <summary>The prefab name of the local player's selected piece, taken before the tables change.</summary>
        public static string SelectedName()
        {
            Player player = Player.m_localPlayer;
            Piece piece = player != null ? player.GetSelectedPiece() : null;
            return piece != null ? piece.gameObject.name : null;
        }

        /// <summary>Learns newly listed entries, rebuilds the available pieces and restores the selection.</summary>
        public static void Refresh(string selectedName)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            player.UpdateKnownRecipesList();
            player.UpdateAvailablePiecesList();
            Reselect(player, selectedName);
        }

        private static void Reselect(Player player, string name)
        {
            PieceTable table = player.m_buildPieces;
            if (table == null)
                return;
            Piece piece = name == null ? null : table.m_availablePieces.FirstOrDefault(p => p != null && p.gameObject.name == name);
            if (piece != null)
                player.SetSelectedPiece(piece);
            else if (name != null || player.GetSelectedPiece() == null)
                player.SetSelectedPiece(Vector2Int.zero);
        }
    }

    /// <summary>
    /// EarthWright's entries are tools, not discoveries: the player learns them silently instead of through one
    /// "new piece" message each (a dozen at the first spawn with a hoe).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownPiece))]
    public static class SilentUnlockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Piece piece)
        {
            if (piece == null || !EntryRegistry.IsOurs(piece.gameObject.name))
                return true;
            __instance.m_knownRecipes.Add(piece.m_name);
            return false;
        }
    }
}
