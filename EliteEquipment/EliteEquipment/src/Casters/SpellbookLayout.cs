using UnityEngine;
using UnityEngine.UI;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Where the Spellbook window's parts sit. The window takes the crafting panel's own rect (anchors, pivot, place and
    /// size; 670 x 740 units, anchored top right, with the inventory mods of 2026-11-02), so it covers that panel wherever
    /// the game or a UI mod puts it, at any resolution and UI scale. Inside, every part is anchored to the window's edges
    /// or to a share of its width, so a bigger or smaller panel only stretches them: the header (title in the crafting
    /// title's place, the book's name, its page count, a separator), the pages on the left and the picked page on the
    /// right, both down to the window's bottom pad.
    /// </summary>
    public static class SpellbookLayout
    {
        /// <summary>Inside the panel's rect on every side (the game's own parts sit 10 in).</summary>
        public const float Pad = 12f;

        /// <summary>Between the two columns.</summary>
        public const float Gap = 8f;

        /// <summary>The page list's share of the width (the recipe list's is 0.35; a little more for the costs).</summary>
        public const float Split = 0.4f;

        /// <summary>From the top: the book's name, its page count, the separator, and the columns' top.</summary>
        public const float NameTop = 52f;
        public const float NameHeight = 28f;
        public const float CountTop = 80f;
        public const float CountHeight = 22f;
        public const float SeparatorTop = 104f;
        public const float BodyTop = 114f;

        /// <summary>Gives the window the crafting panel's rect, when it differs (a cheap check each frame it is open).</summary>
        public static void Fit(RectTransform window, RectTransform crafting)
        {
            if (window == null || crafting == null)
                return;
            if (window.anchoredPosition == crafting.anchoredPosition && window.sizeDelta == crafting.sizeDelta
                && window.anchorMin == crafting.anchorMin && window.anchorMax == crafting.anchorMax && window.pivot == crafting.pivot)
                return;
            window.anchorMin = crafting.anchorMin;
            window.anchorMax = crafting.anchorMax;
            window.pivot = crafting.pivot;
            window.anchoredPosition = crafting.anchoredPosition;
            window.sizeDelta = crafting.sizeDelta;
            window.localScale = crafting.localScale;
        }

        /// <summary>Across the window inside the pads (or a given side inset), from top units under its top edge, that tall.</summary>
        public static void Band(RectTransform rect, float top, float height, float side = Pad)
        {
            CasterUiCopies.Stretch(rect, new Vector2(0f, 1f), Vector2.one, side, -(top + height), side, top);
        }

        /// <summary>
        /// A column of the body on the recipe list's panel: from a share of the width to another, from the header down to
        /// bottom units above the window's bottom edge.
        /// </summary>
        public static RectTransform Column(RectTransform window, string name, float from, float to, float bottom)
        {
            Image panel = CasterUiCopies.Panel(ListPanel(), window, name);
            float left = from <= 0f ? Pad : Gap * 0.5f;
            float right = to >= 1f ? Pad : Gap * 0.5f;
            CasterUiCopies.Stretch(panel.rectTransform, new Vector2(from, 0f), new Vector2(to, 1f), left, bottom, right, BodyTop);
            return panel.rectTransform;
        }

        /// <summary>The recipe list's own panel image (its scroll view's), behind the game's recipe rows.</summary>
        private static Image ListPanel()
        {
            Transform list = SpellbookUi.Gui.m_recipeListRoot;
            return list != null && list.parent != null ? list.parent.GetComponent<Image>() : null;
        }
    }
}
