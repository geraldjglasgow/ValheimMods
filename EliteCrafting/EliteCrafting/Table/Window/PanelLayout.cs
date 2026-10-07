using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// Arranges the copied crafting panel for the Rune Table (rune-table.md section 6), once per copy: the tabs side by
    /// side as the game spaces its Craft and Upgrade tabs, the list's top level with the description's (another mod may
    /// have pushed it down for its own bar), and, between the description text and the requirement slots, two labelled
    /// rows of small slots: the runes and the essences. The small "Style" button sits under the name.
    /// </summary>
    internal static class PanelLayout
    {
        public const int TabCount = 3;
        private const float LabelHeight = 22f;
        private const float SlotSize = 44f;
        private const float Gap = 6f;

        public static void Arrange(PanelParts parts, InventoryGui gui, Action<int> tab, Action<int> rune, Action<int> essence)
        {
            MakeTabs(parts, tab);
            AlignList(parts);
            parts.List = new ListRows(gui.m_recipeElementPrefab, parts.Scroll, gui.m_recipeListSpace);
            parts.Cost = new CostRow(parts.Requirements);
            MakeRows(parts, rune, essence);
            PlaceExtra(parts);
        }

        private static void MakeTabs(PanelParts parts, Action<int> clicked)
        {
            Button? template = parts.TabTemplate;
            if (template == null)
            {
                return;
            }
            var rect = (RectTransform)template.transform;
            float step = rect.rect.width * 1.07f;
            var tabs = new Button[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                Button tab = i == 0 ? template : Object.Instantiate(template, rect.parent, false);
                ((RectTransform)tab.transform).anchoredPosition = rect.anchoredPosition + new Vector2(i * step, 0f);
                tab.onClick = new Button.ButtonClickedEvent();
                int index = i;
                tab.onClick.AddListener(() => clicked(index));
                tabs[i] = tab;
            }
            parts.Tabs = tabs;
        }

        private static void AlignList(PanelParts parts)
        {
            var list = parts.Scroll != null ? parts.Scroll.transform.parent as RectTransform : null;
            if (list == null || parts.Description == null || list.parent != parts.Description.parent)
            {
                return;
            }
            float shift = Rects.FromTopLeft(list).y - Rects.FromTopLeft(parts.Description).y;
            if (shift > 0f)
            {
                list.offsetMax += new Vector2(0f, shift);
            }
        }

        // Text, then "Rune" and its row, then "Essence" and its row, ending just above the requirement slots.
        private static void MakeRows(PanelParts parts, Action<int> rune, Action<int> essence)
        {
            GameObject? template = parts.Cost?.Template;
            if (parts.Text == null || parts.Requirements == null || parts.Description == null || template == null)
            {
                return;
            }
            Rect text = Rects.Pin(parts.Text.rectTransform);
            float bottom = Rects.FromTopLeft(parts.Requirements).y - Gap;
            float top = bottom - 2f * (LabelHeight + SlotSize + Gap);
            Rects.Place(parts.Text.rectTransform, text.x, text.y, text.width, Mathf.Max(60f, top - text.y - Gap));
            parts.Text.overflowMode = TextOverflowModes.Ellipsis;
            float y = top;
            parts.RuneLabel = Label(parts, text.x, y, text.width);
            parts.Runes = new SlotRow(parts.Description, template, Rules.StoneCatalog.BuiltInIds.Length,
                new Rect(text.x, y + LabelHeight, text.width, SlotSize), rune);
            y += LabelHeight + SlotSize + Gap;
            parts.EssenceLabel = Label(parts, text.x, y, text.width);
            // The pure essence item first (what you have), then the essences to choose from.
            parts.Essences = new SlotRow(parts.Description, template, Tables.Essences.All.Length + 1,
                new Rect(text.x, y + LabelHeight, text.width, SlotSize), essence);
        }

        // A copy of the description text, small, in the colour of the item name above it.
        private static TMP_Text Label(PanelParts parts, float x, float y, float width)
        {
            TMP_Text label = Object.Instantiate(parts.Text!, parts.Description, false);
            label.name = "ECF_Label";
            Rects.Place(label.rectTransform, x, y, width, LabelHeight);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.fontSize = Mathf.Min(label.fontSize, LabelHeight - 4f);
            if (parts.Name != null)
            {
                label.color = parts.Name.color;
            }
            return label;
        }

        private static void PlaceExtra(PanelParts parts)
        {
            if (parts.Extra == null)
            {
                return;
            }
            // The game's own listener (the style dialog) goes first, whatever else is missing.
            parts.Extra.onClick = new Button.ButtonClickedEvent();
            if (parts.Name != null)
            {
                Rect name = Rects.FromTopLeft(parts.Name.rectTransform);
                Rects.Place((RectTransform)parts.Extra.transform, name.x, name.y + name.height, 160f, 26f);
            }
        }
    }
}
