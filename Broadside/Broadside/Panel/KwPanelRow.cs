using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// One line of the ship panel on screen: a label on the left, a value on the right, the game's own tooltip (the
    /// inventory item tooltip) on hover, and if it has one a thin bar along the bottom that fills while something runs
    /// (a cooldown, a reload). The whole line catches the pointer for the tooltip (<see cref="KwPanelHover"/>). Every
    /// write is compared with what the line already shows, so an unchanged refresh touches no text.
    /// </summary>
    internal sealed class KwPanelRow
    {
        private const float BarHeight = 2f;

        private readonly RectTransform rect;
        private readonly TMP_Text label;
        private readonly TMP_Text value;
        private readonly UITooltip? tip;
        private RectTransform? track;
        private RectTransform? fill;
        private float filled = -1f;

        // What the line was last drawn from, so a refresh with the same numbers formats nothing.
        private int shownA = int.MinValue, shownB = int.MinValue;
        private string? shownName, shownKey;

        public float Height { get; }

        public KwPanelRow(RectTransform parent, string name, float height, TMP_FontAsset? font, GameObject? tipPrefab)
        {
            Height = height;
            rect = KwPanelLook.Node(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            label = KwPanelLook.Text(rect, "label", font, TextAlignmentOptions.MidlineLeft, KwPanelLook.Label);
            value = KwPanelLook.Text(rect, "value", font, TextAlignmentOptions.MidlineRight, Color.white);
            if (tipPrefab == null)
                return;
            tip = rect.gameObject.AddComponent<UITooltip>();
            tip.m_tooltipPrefab = tipPrefab;
        }

        /// <summary>The bar: a faint track the width of the line with the game's orange filling it, hidden until used.</summary>
        public void AddBar()
        {
            track = KwPanelLook.Block("bar", rect, KwPanelLook.Line).rectTransform;
            track.anchorMin = Vector2.zero;
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(0f, BarHeight);
            fill = KwPanelLook.Block("fill", track, KwPanelLook.Bar).rectTransform;
            KwPanelLook.Stretch(fill);
            track.gameObject.SetActive(false);
        }

        /// <summary>
        /// Whether the line's inputs differ from the last ones drawn (numbers already rounded to what the line shows, plus
        /// up to two strings); records them when they do, so the caller formats only then.
        /// </summary>
        public bool Changed(int a, int b, string? name = null, string? key = null)
        {
            if (a == shownA && b == shownB && shownName == name && shownKey == key)
                return false;
            (shownA, shownB, shownName, shownKey) = (a, b, name, key);
            return true;
        }

        /// <summary>The line's words; each written only when it differs from what shows.</summary>
        public void Set(string labelText, string valueText, string topic, string text)
        {
            if (label.text != labelText)
                label.text = labelText;
            if (value.text != valueText)
                value.text = valueText;
            if (tip == null)
                return;
            if (tip.m_topic != topic)
                tip.m_topic = topic;
            if (tip.m_text != text)
                tip.m_text = text;
        }

        /// <summary>How far it has run, 0 to 1; the bar shows only in between. Written only when it moved.</summary>
        public void SetBar(float progress)
        {
            if (track == null || fill == null || progress == filled)
                return;
            filled = progress;
            bool running = progress > 0f && progress < 1f;
            if (track.gameObject.activeSelf != running)
                track.gameObject.SetActive(running);
            if (running)
                fill.anchorMax = new Vector2(progress, 1f);
        }

        /// <summary>Shown <paramref name="top"/> below the panel's top edge, inset by <paramref name="side"/> on both sides.</summary>
        public void Place(float top, float side)
        {
            if (!rect.gameObject.activeSelf)
                rect.gameObject.SetActive(true);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-2f * side, Height);
        }

        public void Hide()
        {
            if (rect.gameObject.activeSelf)
                rect.gameObject.SetActive(false);
        }
    }
}
