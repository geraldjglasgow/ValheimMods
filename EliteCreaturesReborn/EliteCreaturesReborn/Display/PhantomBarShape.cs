using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// The shape of one bar in a Phantom row (<see cref="PhantomBars"/>): shrunk to its slot, put back as the game drew it
    /// once the boss's copies are gone, and the boss's coloured stars hidden while it sits among copies that show none.
    /// </summary>
    internal static class PhantomBarShape
    {
        private const float BarHeight = 5f;
        private const float NameScale = 0.42f;

        /// <summary>The star glyphs <see cref="StarRow"/> hangs under the health bar.</summary>
        private const string StarPrefix = "ecr_star_";

        // The bar is scaled rather than resized, so the game's own bar filling works in its usual units. The name is
        // scaled evenly, given the slot's width to fit in, and set down just above its bar.
        public static void Shrink(RectTransform root, float width)
        {
            RectTransform? health = root.Find("Health") as RectTransform;
            RectTransform? name = root.Find("Name") as RectTransform;
            if (health == null || name == null || health.sizeDelta.x < 1f || health.sizeDelta.y < 1f)
            {
                return;
            }
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
            RectTransform? health = root != null ? root.Find("Health") as RectTransform : null;
            RectTransform? name = root != null ? root.Find("Name") as RectTransform : null;
            RectTransform? healthShape = template.Find("Health") as RectTransform;
            RectTransform? nameShape = template.Find("Name") as RectTransform;
            if (root == null || health == null || name == null || healthShape == null || nameShape == null
                || health.localScale == healthShape.localScale)
            {
                return;
            }
            root.anchoredPosition = template.anchoredPosition;
            health.localScale = healthShape.localScale;
            name.localScale = nameShape.localScale;
            name.sizeDelta = nameShape.sizeDelta;
            name.anchoredPosition = nameShape.anchoredPosition;
        }

        public static void ShowStars(GameObject gui, bool shown)
        {
            Transform? health = gui != null ? gui.transform.Find("Health") : null;
            if (health == null)
            {
                return;
            }
            for (int i = 0; i < health.childCount; i++)
            {
                GameObject child = health.GetChild(i).gameObject;
                if (child.activeSelf != shown && child.name.StartsWith(StarPrefix, System.StringComparison.Ordinal))
                {
                    child.SetActive(shown);
                }
            }
        }
    }
}
