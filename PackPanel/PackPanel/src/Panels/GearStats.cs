using PackPanel.Core;
using PackPanel.Look;
using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Panels
{
    /// <summary>
    /// The Gear tab's stat sheet between the worn slots' two columns (the user's requests, 2026-09-28): a slightly darker
    /// inset with round corners (<see cref="RoundedFill"/>) holding every stat (<see cref="SheetLines"/>: the core numbers,
    /// the game's resistances, the gear's other modifiers and, with Epic Loot, its magic effects) as small rows
    /// (<see cref="StatRows"/>) in a list that scrolls with the mouse wheel (<see cref="SheetScroll"/>). A child of the
    /// slot panel, shown with the Gear tab. While shown, a few times a second, what the sheet is worked out from is
    /// fingerprinted (<see cref="SheetInputs"/>); the sheet is worked out again only when that changed, or every
    /// <see cref="Recheck"/> seconds for what the fingerprint cannot see, and drawn again only when a line changed.
    /// </summary>
    public static class GearStats
    {
        private const string Name = "PackPanel_gearstats";
        private const float Inset = 6f;
        private const float Every = 0.25f;

        /// <summary>Seconds after which the sheet is worked out again even with the same inputs (another mod's effect changing its numbers).</summary>
        private const float Recheck = 2f;
        private static readonly Color Fill = new Color(0.05f, 0.035f, 0.02f, 0.45f);

        private static readonly StatSheet sheet = new StatSheet();
        private static RectTransform content;
        private static TMP_FontAsset font;
        private static float width;
        private static string shown;
        private static float next;
        private static long shownInputs;
        private static float recheck;

        /// <summary>Over the columns between the first and the last, as tall as the tab's rows; hidden on the other tab.</summary>
        public static void Place(InventoryGui gui, RectTransform panel, SlotPanelLayout plan, float step, TMP_FontAsset textFont)
        {
            RectTransform area = panel.Find(Name) as RectTransform ?? Make(gui, panel);
            content = (RectTransform)area.Find("viewport/content");
            font = textFont;
            area.gameObject.SetActive(plan.Tab == SlotTab.Gear);
            float trim = step - Column.BoxSize;
            area.anchoredPosition = SlotPanel.CellPosition(new Vector2Int(1, 0), step);
            area.sizeDelta = new Vector2((plan.Columns - 2) * step - trim, plan.ContentRows * step - trim);
            width = area.sizeDelta.x - Inset * 2f;
            shown = null;
            next = 0f;
            recheck = 0f;
        }

        /// <summary>Every frame the grid is drawn: while shown, a few times a second, the sheet worked out again when its inputs changed.</summary>
        public static void Refresh()
        {
            float now = Time.unscaledTime;
            if (content == null || now < next || !content.gameObject.activeInHierarchy)
                return;
            next = now + Every;
            Player player = InventoryState.Player;
            long inputs = SheetInputs.Key(player);
            if (inputs == shownInputs && now < recheck)
                return;
            shownInputs = inputs;
            recheck = now + Recheck;
            SheetLines.Fill(player, sheet);
            string key = sheet.Key;
            if (key == shown)
                return;
            shown = key;
            StatRows.Write(content, sheet, width, font);
        }

        private static RectTransform Make(InventoryGui gui, RectTransform panel)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            RectTransform area = (RectTransform)go.transform;
            area.SetParent(panel, false);
            area.anchorMin = area.anchorMax = area.pivot = new Vector2(0f, 1f);
            Image fill = go.GetComponent<Image>();
            fill.sprite = RoundedFill.Sprite;
            fill.type = Image.Type.Sliced;
            fill.color = Fill;
            fill.raycastTarget = false;
            SheetScroll.Make(gui, area, Inset, Inset);
            return area;
        }
    }
}
