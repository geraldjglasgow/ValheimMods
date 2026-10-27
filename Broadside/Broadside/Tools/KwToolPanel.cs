using System;
using HarmonyLib;
using PatchGuard;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammer's panel at the bottom of the screen made room for a Broadside tool's lines (<see cref="KwToolWords"/>): the
    /// game's description box is about two lines high and shrinks its text to fit, so a tool's five or six lines came out too
    /// small to read. With a tool selected the box grows down from where it starts over the panel's empty requirements row
    /// (a tool costs nothing) and keeps its text between readable sizes; any other piece gets the game's own box back.
    /// Local to the player's screen.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
    public static class KwToolPanel
    {
        private const float Height = 132f, FontMin = 11f, FontMax = 15f;
        private static (Vector2 Pivot, Vector2 Position, Vector2 Size, float Min, float Max)? kept;

        [HarmonyPostfix]
        private static void Postfix(Hud __instance, Piece piece) => Guard.Run("Broadside tool panel", () => Fit(__instance.m_pieceDescription, IsTool(piece)));

        private static bool IsTool(Piece? piece) => piece != null && piece.name.StartsWith(KwToolPieces.ToolPrefix, StringComparison.Ordinal);

        // The box grown for a tool, or as the game made it for anything else.
        private static void Fit(TMP_Text text, bool tool)
        {
            RectTransform rect = text.rectTransform;
            kept ??= (rect.pivot, rect.anchoredPosition, rect.sizeDelta, text.fontSizeMin, text.fontSizeMax);
            var game = kept.Value;
            if (!tool)
            {
                (rect.pivot, rect.anchoredPosition, rect.sizeDelta) = (game.Pivot, game.Position, game.Size);
                (text.fontSizeMin, text.fontSizeMax) = (game.Min, game.Max);
                return;
            }
            float top = game.Position.y + game.Size.y * (1f - game.Pivot.y);
            rect.pivot = new Vector2(game.Pivot.x, 1f);
            rect.anchoredPosition = new Vector2(game.Position.x, top);
            rect.sizeDelta = new Vector2(game.Size.x, Height);
            (text.fontSizeMin, text.fontSizeMax) = (FontMin, FontMax);
        }
    }
}
