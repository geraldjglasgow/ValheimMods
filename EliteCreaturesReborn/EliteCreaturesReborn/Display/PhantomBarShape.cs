using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// The shape of one bar in a Phantom row (<see cref="PhantomBars"/>): shrunk to its slot, put back as the game drew it
    /// once the boss's copies are gone, and the boss's coloured stars hidden while it sits among copies that show none.
    /// The row is laid out every frame, so each bar's parts are found once and kept beside it, and a bar already shrunk
    /// to its slot's width is not written again.
    /// </summary>
    internal static class PhantomBarShape
    {
        private const float BarHeight = 5f;
        private const float NameScale = 0.42f;

        /// <summary>The star glyphs <see cref="StarRow"/> hangs under the health bar.</summary>
        private const string StarPrefix = "ecr_star_";

        // A bar's health and name, the width it was last shrunk to (-1: as the game drew it), and its star glyphs,
        // found again whenever the health bar's child count changes.
        private sealed class Parts
        {
            public RectTransform? Health;
            public RectTransform? Name;
            public float Width = -1f;
            public int Children = -1;
            public readonly List<GameObject> Stars = new List<GameObject>();
        }

        private static readonly ConditionalWeakTable<Transform, Parts> Kept = new ConditionalWeakTable<Transform, Parts>();

        /// <summary>The boss bar's health bar on the game's template, or null.</summary>
        public static RectTransform? HealthOf(RectTransform root) => PartsOf(root).Health;

        // The bar is scaled rather than resized, so the game's own bar filling works in its usual units. The name is
        // scaled evenly, given the slot's width to fit in, and set down just above its bar.
        public static void Shrink(RectTransform root, float width)
        {
            Parts parts = PartsOf(root);
            RectTransform? health = parts.Health;
            RectTransform? name = parts.Name;
            if (health == null || name == null || parts.Width == width || health.sizeDelta.x < 1f || health.sizeDelta.y < 1f)
            {
                return;
            }
            parts.Width = width;
            health.localScale = new Vector3(width / health.sizeDelta.x, BarHeight / health.sizeDelta.y, 1f);
            name.localScale = new Vector3(NameScale, NameScale, 1f);
            name.sizeDelta = new Vector2(width / NameScale, name.sizeDelta.y);
            float barCentre = (health.anchorMin.y - name.anchorMin.y) * root.rect.height + health.anchoredPosition.y;
            float lift = BarHeight / 2f + name.sizeDelta.y * NameScale / 2f;
            name.anchoredPosition = new Vector2(0f, barCentre + lift);
        }

        /// <summary>A bar the row shrank, put back from the game's own boss bar template; an untouched bar is left alone.</summary>
        public static void Restore(GameObject gui, RectTransform template)
        {
            RectTransform? root = gui != null ? gui.transform as RectTransform : null;
            Parts? parts = root != null ? PartsOf(root) : null;
            Parts shape = PartsOf(template);
            if (root == null || parts == null || parts.Width < 0f || parts.Health == null || parts.Name == null
                || shape.Health == null || shape.Name == null)
            {
                return;
            }
            parts.Width = -1f;
            root.anchoredPosition = template.anchoredPosition;
            parts.Health.localScale = shape.Health.localScale;
            parts.Name.localScale = shape.Name.localScale;
            parts.Name.sizeDelta = shape.Name.sizeDelta;
            parts.Name.anchoredPosition = shape.Name.anchoredPosition;
        }

        public static void ShowStars(GameObject gui, bool shown)
        {
            Parts? parts = gui != null ? PartsOf(gui.transform) : null;
            Transform? health = parts?.Health;
            if (parts == null || health == null)
            {
                return;
            }
            if (health.childCount != parts.Children)
            {
                parts.Children = health.childCount;
                FindStars(health, parts.Stars);
            }
            foreach (GameObject star in parts.Stars)
            {
                if (star != null && star.activeSelf != shown)
                {
                    star.SetActive(shown);
                }
            }
        }

        private static void FindStars(Transform health, List<GameObject> stars)
        {
            stars.Clear();
            for (int i = 0; i < health.childCount; i++)
            {
                GameObject child = health.GetChild(i).gameObject;
                if (child.name.StartsWith(StarPrefix, System.StringComparison.Ordinal))
                {
                    stars.Add(child);
                }
            }
        }

        private static Parts PartsOf(Transform root)
        {
            if (!Kept.TryGetValue(root, out Parts parts))
            {
                parts = new Parts { Health = root.Find("Health") as RectTransform, Name = root.Find("Name") as RectTransform };
                Kept.Add(root, parts);
            }
            return parts;
        }
    }
}
