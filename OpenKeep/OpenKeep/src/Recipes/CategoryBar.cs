using System.Collections.Generic;
using PlateColumn;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The category row under the search row (<see cref="SearchBar"/>, which places it and gives up the list's height
    /// for it): one square button per <see cref="RecipeCategory"/>, copies of the game's quality - button like the
    /// search row's own, each with OpenKeep's drawn white icon (assets/category_&lt;name&gt;.png, from
    /// ValheimAssets/Assets/Icons/RecipeCategories) and the game's tooltip. The icons are tinted: all light while none is
    /// picked, then the picked ones in the game's orange and the rest dimmed. The buttons share the list's width, at most
    /// as tall as the search row.
    /// </summary>
    public static class CategoryBar
    {
        private const float MaxSize = 30f;
        private const float MinGap = 2f;
        private const float IconInset = 4f;
        private static readonly Color Light = new Color(0.92f, 0.9f, 0.86f, 1f);
        private static readonly Color Dim = new Color(0.5f, 0.5f, 0.5f, 0.75f);

        private static readonly Dictionary<RecipeCategory, Sprite> sprites = new Dictionary<RecipeCategory, Sprite>();
        private static readonly List<Image> icons = new List<Image>();
        private static RectTransform row;

        public static float Height { get; private set; }

        public static bool Shown => row != null && row.gameObject.activeSelf;

        /// <summary>Beside the search row, under the same parent; hidden until placed.</summary>
        public static void Create(InventoryGui gui, Transform parent, int siblingIndex)
        {
            icons.Clear();
            Button template = gui != null ? gui.m_qualityLevelDown : null;
            if (template == null)
                return;
            GameObject go = new GameObject("OpenKeep_RecipeCategories", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(siblingIndex);
            row = (RectTransform)go.transform;
            foreach (RecipeCategory category in RecipeCategories.All)
                AddButton(template, category);
            go.SetActive(false);
        }

        private static void AddButton(Button template, RecipeCategory category)
        {
            Button button = PanelButton.Clone(template, row, "OpenKeep_RecipeCategory_" + category, () => RecipeCategories.Toggle(category));
            icons.Add(PanelButton.Icon(button, Sprite(category), IconInset));
            RecipeTips.Set(button.gameObject, RecipeWords.CategoryTopic(category), RecipeWords.CategoryTip(category));
        }

        /// <summary>At InventoryGui.Awake: the old row went with the old inventory.</summary>
        public static void Reset()
        {
            row = null;
            icons.Clear();
            Height = 0f;
        }

        public static void Show(bool on)
        {
            if (row != null && row.gameObject.activeSelf != on)
                row.gameObject.SetActive(on);
        }

        /// <summary>
        /// Top left at x, y in the list's parent, as wide as the list: square buttons as large as fit (at most the search
        /// row's height), spread over the width.
        /// </summary>
        public static void Place(float x, float y, float width)
        {
            if (row == null)
                return;
            int count = icons.Count;
            Height = Mathf.Floor(Mathf.Min(MaxSize, (width - MinGap * (count - 1)) / count));
            PanelButton.PlaceTopLeft(row, x, y, new Vector2(width, Height));
            float gap = count > 1 ? (width - Height * count) / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
                PanelButton.PlaceTopLeft((RectTransform)icons[i].transform.parent, i * (Height + gap), 0f, new Vector2(Height, Height));
        }

        /// <summary>After every panel update while the row shows: each icon's tint from what is picked.</summary>
        public static void Refresh()
        {
            bool filtering = RecipeCategories.Filtering;
            for (int i = 0; i < icons.Count; i++)
            {
                bool picked = RecipeCategories.IsPicked(RecipeCategories.All[i]);
                icons[i].color = !filtering ? Light : picked ? RecipeStar.On : Dim;
            }
        }

        /// <summary>The embedded icon, loaded once per game.</summary>
        private static Sprite Sprite(RecipeCategory category)
        {
            if (sprites.TryGetValue(category, out Sprite known) && known != null)
                return known;
            string name = "category_" + category.ToString().ToLowerInvariant();
            Sprite sprite = EmbeddedSprite.Load(typeof(CategoryBar).Assembly, "OpenKeep.assets." + name + ".png", "OpenKeep_" + name);
            sprites[category] = sprite;
            return sprite;
        }
    }
}
