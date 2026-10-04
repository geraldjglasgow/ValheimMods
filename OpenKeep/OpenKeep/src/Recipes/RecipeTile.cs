using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Reshapes one of the game's freshly built recipe rows (children bkg, selected, icon, name, Durability,
    /// QualityLevel). A tile is square: the icon fills it less a 3 unit border, the name is hidden (the tile's tooltip
    /// shows it), the quality level sits small in the top left corner and the durability bar along the bottom. The game
    /// hides an uncraftable recipe's icon in its list, where the grey name says it; a tile shows the icon greyed. A
    /// compact row is the game's row made lower, its icon and marks scaled with it.
    /// </summary>
    public static class RecipeTile
    {
        private const float Border = 3f;
        private static readonly Color Unavailable = new Color(0.45f, 0.45f, 0.45f, 0.8f);

        public static void Shape(InventoryGui.RecipeDataPair pair, float size)
        {
            Transform row = pair.InterfaceElement.transform;
            ((RectTransform)row).sizeDelta = new Vector2(size, size);
            TileIcon(row.Find("icon"), pair.CanCraft);
            Transform name = row.Find("name");
            if (name != null)
                name.gameObject.SetActive(false);
            TileLevel(row.Find("QualityLevel"), size);
            TileDurability(row.Find("Durability"), size);
        }

        private static void TileIcon(Transform icon, bool canCraft)
        {
            if (icon == null)
                return;
            RectTransform rect = (RectTransform)icon;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(Border, Border);
            rect.offsetMax = new Vector2(-Border, -Border);
            Image image = icon.GetComponent<Image>();
            if (image != null && !canCraft)
                image.color = Unavailable;
        }

        private static void TileLevel(Transform level, float size)
        {
            if (level == null)
                return;
            RectTransform rect = (RectTransform)level;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Border, -1f);
            rect.sizeDelta = new Vector2(size * 0.5f, size * 0.4f);
            TMP_Text text = level.GetComponent<TMP_Text>();
            if (text == null)
                return;
            text.enableAutoSizing = false;
            text.fontSize = Mathf.Round(size * 0.3f);
            text.alignment = TextAlignmentOptions.TopLeft;
        }

        /// <summary>The game's bar keeps the width it woke with, so it is scaled rather than resized.</summary>
        private static void TileDurability(Transform bar, float size)
        {
            if (bar == null)
                return;
            RectTransform rect = (RectTransform)bar;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, Border + 1f);
            float scale = Mathf.Min(1f, (size - 2f * Border) / Mathf.Max(1f, rect.sizeDelta.x));
            rect.localScale = new Vector3(scale, 1f, 1f);
        }

        /// <summary>A compact row: as high as the step plus the game's 2 unit overlap, everything in it scaled to match.</summary>
        public static void Compact(RectTransform row, float step)
        {
            float height = step + 2f;
            float scale = height / Mathf.Max(1f, row.sizeDelta.y);
            row.sizeDelta = new Vector2(row.sizeDelta.x, height);
            RectTransform icon = row.Find("icon") as RectTransform;
            if (icon == null)
                return;
            float half = row.sizeDelta.x / 2f;
            float oldCentre = icon.anchoredPosition.x + icon.sizeDelta.x / 2f - half;
            icon.sizeDelta *= scale;
            float newCentre = icon.anchoredPosition.x + icon.sizeDelta.x / 2f - half;
            CompactName(row.Find("name") as RectTransform, scale);
            CompactMark(row.Find("QualityLevel") as RectTransform, oldCentre, newCentre, scale);
            CompactMark(row.Find("Durability") as RectTransform, oldCentre, newCentre, scale);
        }

        /// <summary>The name starts nearer the smaller icon and gains the width that frees.</summary>
        private static void CompactName(RectTransform name, float scale)
        {
            if (name == null)
                return;
            float x = name.anchoredPosition.x;
            name.anchoredPosition = new Vector2(x * scale, name.anchoredPosition.y);
            name.sizeDelta = new Vector2(name.sizeDelta.x + x - x * scale, name.sizeDelta.y * scale);
        }

        /// <summary>A mark placed relative to the icon's centre (the row's centre is its anchor) keeps its place on the smaller icon.</summary>
        private static void CompactMark(RectTransform mark, float oldCentre, float newCentre, float scale)
        {
            if (mark == null)
                return;
            Vector2 at = mark.anchoredPosition;
            mark.anchoredPosition = new Vector2(newCentre + (at.x - oldCentre) * scale, at.y * scale);
            mark.localScale = mark.localScale * scale;
        }
    }
}
