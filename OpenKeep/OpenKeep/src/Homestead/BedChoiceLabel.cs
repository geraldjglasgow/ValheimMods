using OpenKeep.Core;
using TMPro;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The countdown of the choice of bed, in the upper left corner of the large map (the user's call, 2026-09-30, moved
    /// from the upper right the same day): the seconds left in large figures, then what happens when they run out, how
    /// to choose, and the keys. A child of the map image, anchored to its top left corner and drawn after the map's own
    /// children; left aligned, in the font, material and colour of the game's biome name, so it reads as the game's.
    /// Only the seconds change, and the text is rebuilt only when they do.
    /// </summary>
    public static class BedChoiceLabel
    {
        private const float Inset = 18f;
        private const float Width = 560f;
        private const float Height = 220f;
        private const float FontShare = 0.5f;
        private const string Figures = "<size=220%>{0}</size>";

        private static TMP_Text label;
        private static int shownSeconds = -1;

        public static void Show(Minimap map, int seconds)
        {
            TMP_Text text = Get(map);
            text.gameObject.SetActive(true);
            if (seconds == shownSeconds)
                return;
            shownSeconds = seconds;
            text.text = string.Format(Figures, seconds) + "\n" + Language.Localize(BedFeature.ChoiceNearest) + "\n"
                + Language.Localize(BedFeature.ChoiceClick) + "\n" + Language.Localize(BedFeature.ChoiceKeys);
        }

        public static void Hide()
        {
            if (label != null)
                label.gameObject.SetActive(false);
            shownSeconds = -1;
        }

        private static TMP_Text Get(Minimap map)
        {
            Transform parent = map.m_mapImageLarge.transform;
            if (label != null && label.transform.parent == parent)
                return label;
            RectTransform rect = (RectTransform)new GameObject("OpenKeep_BedChoice", typeof(RectTransform)).transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(Inset, -Inset);
            rect.sizeDelta = new Vector2(Width, Height);
            rect.gameObject.SetActive(false);   // the text wakes with its font set, so it never looks for TextMeshPro's missing default
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = map.m_biomeNameLarge.font;
            rect.gameObject.SetActive(true);
            label = Style(text, map.m_biomeNameLarge);
            shownSeconds = -1;
            return label;
        }

        private static TMP_Text Style(TMP_Text text, TMP_Text like)
        {
            text.font = like.font;
            text.fontSharedMaterial = like.fontSharedMaterial;
            text.color = like.color;
            text.fontSize = like.fontSize * FontShare;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }
    }
}
