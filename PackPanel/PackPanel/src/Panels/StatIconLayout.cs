using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Panels
{
    /// <summary>Centers live values over icons in compact squares and restores the previous layout when disabled.</summary>
    public static class StatIconLayout
    {
        public const float Size = 48f;
        private static readonly Dictionary<RectTransform, Action> rects = new Dictionary<RectTransform, Action>();
        private static readonly Dictionary<TMP_Text, Action> texts = new Dictionary<TMP_Text, Action>();

        public static void Apply(RectTransform box, Image background, TMP_Text value, bool weight)
        {
            Remember(box);
            box.sizeDelta = new Vector2(Size, Size);
            foreach (Transform child in box)
            {
                Image icon = child.GetComponent<Image>();
                if (icon != null && icon != background) Center(icon.rectTransform, new Vector2(32f, 32f));
            }
            if (value == null) return;
            Transform line = value.transform;
            while (line.parent != box) line = line.parent;
            Vector2 textSize = new Vector2(32f, weight ? 34f : 24f);
            Center((RectTransform)line, textSize);
            if (value.transform != line) Center(value.rectTransform, textSize);
            Font(value);
        }

        private static void Font(TMP_Text value)
        {
            if (!texts.ContainsKey(value))
            {
                float min = value.fontSizeMin, max = value.fontSizeMax;
                bool auto = value.enableAutoSizing;
                TextAlignmentOptions alignment = value.alignment;
                texts[value] = () =>
                {
                    value.fontSizeMin = min; value.fontSizeMax = max;
                    value.enableAutoSizing = auto; value.alignment = alignment;
                };
            }
            value.alignment = TextAlignmentOptions.Center;
            value.enableAutoSizing = true;
            value.fontSizeMin = 8f;
            value.fontSizeMax = 11f;
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            Remember(rect);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void Remember(RectTransform rect)
        {
            if (rects.ContainsKey(rect)) return;
            Vector2 min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot;
            Vector2 at = rect.anchoredPosition, size = rect.sizeDelta;
            rects[rect] = () =>
            {
                rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
                rect.anchoredPosition = at; rect.sizeDelta = size;
            };
        }

        public static void Restore()
        {
            foreach (var pair in rects) if (pair.Key != null) pair.Value();
            foreach (var pair in texts) if (pair.Key != null) pair.Value();
            rects.Clear();
            texts.Clear();
        }
    }
}
