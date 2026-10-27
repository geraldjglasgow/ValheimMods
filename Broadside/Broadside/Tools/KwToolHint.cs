using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// While a Broadside tool is in the hammer (or a Broadside window is open), the messages the game would show in the
    /// middle of the screen (every tool's hints as the aim moves, what a click built or why not) show instead on one small
    /// line near the top, out of the way of the work and the windows; they fade a few seconds after the last one. Every
    /// other message, a launched ship's included, shows where the game shows it. Caught where every centre message goes
    /// (the message box itself, so the tools' hover hints that skip the player's message call and its log are caught too).
    /// Local to the player's screen.
    /// </summary>
    [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
    public static class KwToolHint
    {
        private const float Size = 0.55f, Below = 64f, Width = 1400f;
        private static TextMeshProUGUI? label;

        /// <summary>When the line's text was last set (its fade counts from it).</summary>
        internal static float Since { get; private set; }

        [HarmonyPrefix]
        private static bool Prefix(MessageHud.MessageType type, string text)
        {
            try
            {
                return !(Takes(Player.m_localPlayer, type) && Show(text));
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside tool hint: {e.Message}");
                return true;
            }
        }

        // A centre message to this player while a Broadside tool is held or a Broadside window is open.
        private static bool Takes(Player? player, MessageHud.MessageType type)
        {
            if (player == null || type != MessageHud.MessageType.Center)
                return false;
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return KwForm.IsOpen || (piece != null && piece.name.StartsWith(KwToolPieces.ToolPrefix, StringComparison.Ordinal));
        }

        // The text on the line; false when there is no line to show it on (the game's own message shows instead).
        private static bool Show(string msg)
        {
            TextMeshProUGUI? line = Line();
            if (line == null)
                return false;
            line.text = Localization.instance != null ? Localization.instance.Localize(msg) : msg;
            line.alpha = 1f;
            Since = Time.time;
            return true;
        }

        // The line, made once on the HUD's canvas in the game's own message font, smaller, at the top centre.
        private static TextMeshProUGUI? Line()
        {
            TMP_Text? centre = MessageHud.instance != null ? MessageHud.instance.m_messageCenterText : null;
            if (label != null || centre == null || centre.canvas == null)
                return label;
            GameObject made = new GameObject("KwToolHint", typeof(RectTransform));
            made.transform.SetParent(centre.canvas.rootCanvas.transform, false);
            label = made.AddComponent<TextMeshProUGUI>();
            (label.font, label.fontSharedMaterial, label.color) = (centre.font, centre.fontSharedMaterial, centre.color);
            (label.fontSize, label.alignment, label.raycastTarget, label.textWrappingMode) = (centre.fontSize * Size, TextAlignmentOptions.Top, false, TextWrappingModes.Normal);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            (rect.anchoredPosition, rect.sizeDelta) = (new Vector2(0f, -Below), new Vector2(Width, 120f));
            made.AddComponent<KwToolHintFade>();
            return label;
        }
    }

    /// <summary>Fades the tool hint line (<see cref="KwToolHint"/>) a few seconds after its last message, then empties it.</summary>
    public sealed class KwToolHintFade : MonoBehaviour
    {
        private const float Shown = 3.5f, Fade = 0.75f;
        private TextMeshProUGUI? text;

        private void Awake() => text = GetComponent<TextMeshProUGUI>();

        private void Update()
        {
            if (text == null || text.text.Length == 0)
                return;
            float left = Shown + Fade - (Time.time - KwToolHint.Since);
            text.alpha = Mathf.Clamp01(left / Fade);
            if (left <= 0f)
                text.text = "";
        }
    }
}
