using PackPanel.Panels;
using PackPanel.Slots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Consume
{
    /// <summary>
    /// One square of the Food and Mead bar (<see cref="ConsumeBar"/>): a copy of the game's own HUD food square (its dark
    /// square, the icon filling it and its small text) showing the group's slot icon, a food for the Food slots and a mead
    /// for the Mead slots, with the key that eats or drinks from them over its top-left corner in the yellow of the game's
    /// key hints, where the hotbar shows its numbers. Nothing else: no items, no counts (the user's call, 2026-09-29).
    /// </summary>
    public static class ConsumeBarCell
    {
        public const float Size = 42f;
        private const float KeyFont = 18f;
        private const float KeyInset = 3f;
        private static readonly Color KeyColour = new Color(1f, 0.92f, 0.016f, 1f);   // Unity's yellow, the game's key hints

        /// <summary>The square of a group, its left edge at <paramref name="x"/>.</summary>
        public static void Group(RectTransform bar, GameObject template, SlotKind kind, string key, float x)
        {
            RectTransform rect = Copy(bar, template, "PackPanel_" + kind.ToString().ToLowerInvariant(), x);
            Image icon = IconOf(rect);
            if (icon != null)
            {
                icon.sprite = SlotIcons.For(kind);
                icon.color = Color.white;
                icon.enabled = icon.sprite != null;
            }
            TMP_Text label = rect.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                KeyLabel(label, key);
        }

        /// <summary>The square's small text moved to its top-left corner, showing the key in yellow.</summary>
        private static void KeyLabel(TMP_Text label, string key)
        {
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Size, KeyFont + 4f);
            rect.anchoredPosition = new Vector2(KeyInset, -1f);
            label.enableAutoSizing = false;
            label.fontSize = KeyFont;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = KeyColour;
            label.text = key;
        }

        /// <summary>A live copy of the game's food square: shown, its children shown, at x along the bar's bottom.</summary>
        private static RectTransform Copy(RectTransform bar, GameObject template, string name, float x)
        {
            GameObject go = Object.Instantiate(template, bar, false);
            go.name = name;
            go.SetActive(true);
            RectTransform rect = (RectTransform)go.transform;
            foreach (Transform child in rect)
                child.gameObject.SetActive(true);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(Size, Size);
            rect.anchoredPosition = new Vector2(x, 0f);
            return rect;
        }

        /// <summary>The square's icon: its first image below the square itself.</summary>
        private static Image IconOf(RectTransform rect)
        {
            foreach (Image image in rect.GetComponentsInChildren<Image>(true))
            {
                if (image.transform != rect)
                    return image;
            }
            return null;
        }
    }
}
