using OpenKeep.Store;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The favourite mark: the star the game's build menu puts on a favourite piece (its piece button's
    /// <c>m_favoriteStar</c>, sprite and colour), on the corner of a favourite recipe's icon. Before the build menu is
    /// found, or if a game update moves it, OpenKeep's own drawn star in the same orange stands in.
    /// </summary>
    public static class RecipeStar
    {
        private const float ListSize = 12f;
        private const float TileSize = 14f;
        private static readonly Color GameOrange = new Color(1f, 0.713f, 0.361f, 1f);

        public static readonly Color Off = new Color(0.55f, 0.55f, 0.55f, 0.9f);

        private const float RetrySeconds = 2f;

        private static Sprite sprite;
        private static Color colour = GameOrange;
        private static float nextFind;

        public static Sprite Sprite
        {
            get
            {
                if (sprite == null)
                    Find();
                return sprite != null ? sprite : StoreSprites.Star;
            }
        }

        public static Color On
        {
            get
            {
                if (sprite == null)
                    Find();
                return colour;
            }
        }

        /// <summary>A star on the row's icon corner (the row itself when it has no icon); never a raycast target.</summary>
        public static void Add(GameObject row, bool tile)
        {
            Transform icon = row.transform.Find("icon");
            GameObject go = new GameObject("OpenKeep_star", typeof(RectTransform), typeof(Image));
            go.layer = row.layer;
            go.transform.SetParent(icon != null ? icon : row.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(1f, 1f);
            float size = tile ? TileSize : ListSize;
            rect.sizeDelta = new Vector2(size, size);
            Image image = go.GetComponent<Image>();
            image.sprite = Sprite;
            image.color = On;
            image.raycastTarget = false;
        }

        /// <summary>Looked for at most every 2 s until found (the panel asks every frame).</summary>
        private static void Find()
        {
            if (Time.unscaledTime < nextFind)
                return;
            nextFind = Time.unscaledTime + RetrySeconds;
            Image star = GameStar();
            if (star == null || star.sprite == null)
                return;
            sprite = star.sprite;
            colour = star.color;
        }

        private static Image GameStar()
        {
            BuildUi build = Hud.instance != null ? Hud.instance.m_buildUi : null;
            GameObject prefab = build != null ? build.m_pieceButtonPrefab : null;
            BuildUiPieceButton button = prefab != null ? prefab.GetComponent<BuildUiPieceButton>() : null;
            return button != null ? button.m_favoriteStar : null;
        }
    }
}
