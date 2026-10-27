using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's hover in hammer mode, with a tool that removes pieces (the game shows none for a piece there, only its health): with the remove
    /// allowed, "[Middle mouse] break up the ship (its materials go into floating crates)" in the game's key name for its
    /// remove; else why not (<see cref="KwShipBreakRules.Why"/>: someone aboard, a ward, no workbench near). Written after
    /// the game writes the hover text each frame; the line is worked out again at most four times a second. Local to the
    /// player's screen.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public static class KwShipBreakHover
    {
        private const string Allowed = "[<color=yellow><b>$KEY_Remove</b></color>] break up the ship (its materials go into floating crates)";
        private const float Every = 0.25f;
        private static Piece? last, seen;
        private static KwShipBody? seenBody;
        private static float madeAt = -99f;
        private static string line = "";

        [HarmonyPostfix]
        private static void Postfix(Hud __instance, Player player)
        {
            if (player != null && player.InPlaceMode() && player.m_buildPieces != null && player.m_buildPieces.m_canRemovePieces)
                Guard.Run("Broadside ship break hover", Show, __instance, player);
        }

        private static void Show(Hud hud, Player player)
        {
            Piece? piece = player == Player.m_localPlayer ? player.GetHoveringPiece() : null;
            if (piece != seen)
                (seen, seenBody) = (piece, piece != null ? piece.GetComponent<KwShipBody>() : null);
            if (piece != null && seenBody != null)
                hud.m_hoverName.text = Line(player, piece, seenBody);
        }

        private static string Line(Player player, Piece piece, KwShipBody body)
        {
            if (piece == last && Time.time - madeAt < Every)
                return line;
            (last, madeAt) = (piece, Time.time);
            Ship? ship = body.GetComponent<Ship>();
            string? why = ship != null ? KwShipBreakRules.Why(player, piece, ship, body.Hull) : "";
            line = why ?? Localization.instance.Localize(Allowed);
            if (ZInput.IsGamepadActive())
                line = line.Replace("[<color=yellow><b><sprite=", "<sprite=").Replace("\"></b></color>]", "\">");
            return line;
        }
    }
}
