using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShipConfig
{
    /// <summary>
    /// One line of the ship panel: a label on the left, a value on the right, the game's own tooltip (the inventory
    /// item tooltip) on hover, and for an ability a thin bar along the bottom that fills while it cools down. The whole
    /// line catches the pointer for the tooltip (<see cref="PanelHover"/>).
    /// </summary>
    public sealed class PanelRow
    {
        private const float BarHeight = 2f;

        private readonly RectTransform rect;
        private readonly TMP_Text label;
        private readonly TMP_Text value;
        private RectTransform track;
        private RectTransform fill;

        // What the line was last drawn from, so a refresh with the same numbers formats and writes nothing.
        private int shownA = int.MinValue;
        private int shownB = int.MinValue;
        private string shownName;
        private string shownKey;

        public float Height { get; }
        public UITooltip Tip { get; }

        public PanelRow(RectTransform parent, string name, float height, TMP_FontAsset font, GameObject tipPrefab)
        {
            Height = height;
            rect = PanelLook.Node(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            label = PanelLook.Text(rect, "label", font, TextAlignmentOptions.MidlineLeft, PanelLook.Label);
            value = PanelLook.Text(rect, "value", font, TextAlignmentOptions.MidlineRight, Color.white);
            if (tipPrefab == null)
                return;
            Tip = rect.gameObject.AddComponent<UITooltip>();
            Tip.m_tooltipPrefab = tipPrefab;
        }

        /// <summary>The cooldown bar: a faint track the width of the line with the game's orange filling it.</summary>
        public void AddBar()
        {
            track = PanelLook.Block("bar", rect, PanelLook.Line).rectTransform;
            track.anchorMin = Vector2.zero;
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(0f, BarHeight);
            fill = PanelLook.Block("fill", track, PanelLook.Bar).rectTransform;
            PanelLook.Stretch(fill);
            track.gameObject.SetActive(false);
        }

        /// <summary>
        /// Whether the line's inputs differ from the last ones drawn (numbers already rounded to what the line shows,
        /// plus up to two strings); records them when they do, so the caller formats only then.
        /// </summary>
        public bool Changed(int a, int b, string name = null, string key = null)
        {
            if (a == shownA && b == shownB && shownName == name && shownKey == key)
                return false;
            (shownA, shownB, shownName, shownKey) = (a, b, name, key);
            return true;
        }

        public void Set(string labelText, string valueText, string topic, string text)
        {
            label.text = labelText;
            value.text = valueText;
            if (Tip == null)
                return;
            Tip.m_topic = topic;
            Tip.m_text = text;
        }

        /// <summary>How far the cooldown has run, 0 to 1; the bar shows only in between.</summary>
        public void SetBar(float progress)
        {
            if (track == null)
                return;
            bool cooling = progress > 0f && progress < 1f;
            if (track.gameObject.activeSelf != cooling)
                track.gameObject.SetActive(cooling);
            if (cooling)
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
