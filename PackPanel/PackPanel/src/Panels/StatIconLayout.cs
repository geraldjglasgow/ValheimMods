using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Panels
{
    /// <summary>
    /// Centers live values over icons in compact squares and restores the previous layout when disabled. Applied every
    /// frame, so a value is written only when it differs: an unchanged write would still dirty the HUD's canvas. Each
    /// box's images are looked up once (again when its children change in number), not searched for every frame. A
    /// restore puts back only the boxes under the panel it is asked for, so the inventory's never undoes the HUD's.
    /// </summary>
    public static class StatIconLayout
    {
        public const float Size = 48f;
        private static readonly Dictionary<RectTransform, Action> rects = new Dictionary<RectTransform, Action>();
        private static readonly Dictionary<TMP_Text, Action> texts = new Dictionary<TMP_Text, Action>();
        private static readonly Dictionary<RectTransform, Image[]> images = new Dictionary<RectTransform, Image[]>();
        private static readonly List<Component> undone = new List<Component>();

        public static void Apply(RectTransform box, Image background, TMP_Text value, bool weight)
        {
            Remember(box);
            SetSize(box, new Vector2(Size, Size));
            foreach (Image icon in ImagesOf(box))
                if (icon != null && icon != background) Center(icon.rectTransform, new Vector2(32f, 32f));
            if (value == null) return;
            Transform line = value.transform;
            while (line.parent != box) line = line.parent;
            Vector2 textSize = new Vector2(32f, weight ? 34f : 24f);
            Center((RectTransform)line, textSize);
            if (value.transform != line) Center(value.rectTransform, textSize);
            Font(value);
        }

        /// <summary>The image of each direct child of the box (null for a child without one), in child order.</summary>
        private static Image[] ImagesOf(RectTransform box)
        {
            if (images.TryGetValue(box, out Image[] found) && found.Length == box.childCount)
                return found;
            found = new Image[box.childCount];
            for (int i = 0; i < found.Length; i++)
                found[i] = box.GetChild(i).GetComponent<Image>();
            images[box] = found;
            return found;
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
            if (value.alignment != TextAlignmentOptions.Center) value.alignment = TextAlignmentOptions.Center;
            if (!value.enableAutoSizing) value.enableAutoSizing = true;
            if (value.fontSizeMin != 8f) value.fontSizeMin = 8f;
            if (value.fontSizeMax != 11f) value.fontSizeMax = 11f;
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            Remember(rect);
            Vector2 middle = new Vector2(0.5f, 0.5f);
            if (rect.anchorMin != middle) rect.anchorMin = middle;
            if (rect.anchorMax != middle) rect.anchorMax = middle;
            if (rect.pivot != middle) rect.pivot = middle;
            if (rect.anchoredPosition != Vector2.zero) rect.anchoredPosition = Vector2.zero;
            SetSize(rect, size);
        }

        private static void SetSize(RectTransform rect, Vector2 size)
        {
            if (rect.sizeDelta != size) rect.sizeDelta = size;
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

        /// <summary>The previous layout back on everything under <paramref name="under"/> (on everything for null), then forgotten.</summary>
        public static void Restore(Transform under)
        {
            Restore(rects, under);
            Restore(texts, under);
            images.Clear();
        }

        private static void Restore<T>(Dictionary<T, Action> memos, Transform under) where T : Component
        {
            undone.Clear();
            foreach (var pair in memos)
                if (pair.Key == null || under == null || pair.Key.transform.IsChildOf(under)) undone.Add(pair.Key);
            foreach (Component key in undone)
            {
                if (key != null) memos[(T)key]();
                memos.Remove((T)key);
            }
        }
    }
}
