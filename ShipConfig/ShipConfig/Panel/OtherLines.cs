using TMPro;
using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Other mods' lines on the ship panel, stacked under the ship's health. ShipConfig is never referenced: a mod finds the
    /// container <see cref="Name"/> under the HUD root (<c>Hud.instance.m_rootObject</c>, at
    /// <c>ShipConfig_ShipPanel/ShipConfig_ShipPanelRows</c>; it is made the first time the local player is aboard a ship
    /// and hides with the panel), copies its child <see cref="Template"/> (an inactive line in the panel's own look) into it
    /// with <c>Object.Instantiate(template, container, false)</c>, gives the copy a name of its own, activates it and writes
    /// its two texts, <c>label</c> and <c>value</c> (TextMeshPro, rich text), and if it likes the topic and text of its
    /// <c>UITooltip</c>, the game's tooltip on hover. Every active child but the template is a line, in sibling order, each
    /// as high as its RectTransform is (the template's line height unless the mod changes it); the panel lays them out and
    /// sizes its frame in the frame they show or hide, or within the next. A mod hides its line (SetActive(false)) rather
    /// than destroying it; a hidden one takes no room.
    /// </summary>
    public sealed class OtherLines
    {
        public const string Name = "ShipConfig_ShipPanelRows", Template = "Template";

        private readonly RectTransform container;
        private readonly Transform template;
        private int laidOut = int.MinValue;

        public OtherLines(RectTransform root, float height, float side, TMP_FontAsset font, GameObject tipPrefab)
        {
            container = PanelLook.Node(Name, root);
            container.anchorMin = new Vector2(0f, 1f);
            container.anchorMax = new Vector2(1f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            PanelRow line = new PanelRow(container, Template, height, font, tipPrefab);
            line.Place(0f, side);
            line.Hide();
            template = container.GetChild(0);
        }

        /// <summary>The container, whose children are the lines.</summary>
        public Transform Container => container;

        /// <summary>Whether a line was added, shown or hidden since they were last laid out (a few children looked at).</summary>
        public bool Changed => Shown() != laidOut;

        /// <summary>The lines placed from <paramref name="top"/> down, inset by <paramref name="side"/>; returns where the next thing goes.</summary>
        public float Stack(float top, float side)
        {
            laidOut = Shown();
            container.anchoredPosition = new Vector2(0f, -top);
            float y = 0f;
            for (int i = 0; i < container.childCount; i++)
                y = Place(container.GetChild(i) as RectTransform, y, side);
            container.sizeDelta = new Vector2(0f, y);
            return top + y;
        }

        // One active line under the last, as wide as the panel's own; the template and hidden lines take no room.
        private float Place(RectTransform line, float y, float side)
        {
            if (line == null || line == template || !line.gameObject.activeSelf)
                return y;
            float height = line.sizeDelta.y;
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.anchoredPosition = new Vector2(0f, -y);
            line.sizeDelta = new Vector2(-2f * side, height);
            return y + height;
        }

        // Which children there are and which show, as one number: one bit a child after their count.
        private int Shown()
        {
            int shown = container.childCount;
            for (int i = 0; i < container.childCount; i++)
                shown = shown * 2 + (container.GetChild(i).gameObject.activeSelf ? 1 : 0);
            return shown;
        }
    }
}
