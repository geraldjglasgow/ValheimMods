using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The tool's lines at the top of the screen while it is in hand (what it will do, how busy and which scenes, the cost
    /// against what the player has, the keys), under the tools' hint line, in the game's message font. Only drawn, on
    /// this player's screen; empty and hidden when the tool is put away.
    /// </summary>
    public static class KwDecorHud
    {
        private const float Size = 0.5f, Below = 124f, Width = 1400f, Height = 240f;
        private static TextMeshProUGUI? label;
        private static string said = "";

        public static void Show(string text)
        {
            TextMeshProUGUI? line = Line();
            if (line == null || (text == said && line.gameObject.activeSelf))
                return;
            said = text;
            line.text = text;
            line.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (label != null && label.gameObject.activeSelf)
                label.gameObject.SetActive(false);
            said = "";
        }

        // The line, made once on the HUD's canvas in the game's own message font, smaller, at the top centre.
        private static TextMeshProUGUI? Line()
        {
            TMP_Text? centre = MessageHud.instance != null ? MessageHud.instance.m_messageCenterText : null;
            if (label != null || centre == null || centre.canvas == null)
                return label;
            GameObject made = new GameObject("BS_DecorateLines", typeof(RectTransform));
            made.transform.SetParent(centre.canvas.rootCanvas.transform, false);
            label = made.AddComponent<TextMeshProUGUI>();
            (label.font, label.fontSharedMaterial, label.color) = (centre.font, centre.fontSharedMaterial, centre.color);
            (label.fontSize, label.alignment, label.raycastTarget, label.textWrappingMode) = (centre.fontSize * Size, TextAlignmentOptions.Top, false, TextWrappingModes.Normal);
            (label.richText, label.alpha) = (true, 1f);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            (rect.anchoredPosition, rect.sizeDelta) = (new Vector2(0f, -Below), new Vector2(Width, Height));
            return label;
        }
    }
}
