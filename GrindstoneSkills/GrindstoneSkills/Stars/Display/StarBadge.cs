using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A starred dish's stars on its item slot: a column of the game's star (<see cref="StarGlyph"/>) down the slot's
    /// left edge. Inventory, container and hotbar slots share one 64 px layout: the hotkey number in the top-left
    /// corner, the food icon and the no-teleport mark in the top-right, the stack count and the durability bar along
    /// the bottom. The column starts under the hotkey number and ends beside the stack count, covering neither. Glyphs
    /// are made the first time a slot shows that many stars and afterwards only switched on and off; the badge lives
    /// and dies with its slot, which the grid destroys when its size changes.
    /// <para>The grids call <see cref="Apply"/> for every slot every frame, and since rolls give no stars
    /// (<see cref="StarOdds"/>) only items starred before that, or eggs from a starred hen, still have any. While no
    /// badge exists, hiding stars returns before looking at the slot.</para>
    /// </summary>
    public class StarBadge : MonoBehaviour
    {
        private const float Size = 13f;
        private const float Step = 12f;
        private const float CentreX = 8f;
        private const float FirstCentreY = -24f;

        /// <summary>Badges that woke and are not destroyed yet. One added to a hidden slot counts once the slot shows.</summary>
        private static int live;

        private readonly RectTransform[] glyphs = new RectTransform[Stars.Max];
        private int shown;

        /// <summary>Shows <paramref name="stars"/> on the slot. 0 hides them, and adds nothing to a slot without a badge.</summary>
        public static void Apply(GameObject slot, int stars)
        {
            if ((stars <= 0 && live == 0) || slot == null)
                return;
            if (!slot.TryGetComponent(out StarBadge badge))
            {
                if (stars <= 0)
                    return;
                badge = slot.AddComponent<StarBadge>();
            }
            badge.Show(Mathf.Clamp(stars, 0, Stars.Max));
        }

        private void Awake() => live++;

        private void OnDestroy() => live--;

        private void Show(int stars)
        {
            if (stars == shown)
                return;
            for (int i = 0; i < glyphs.Length; i++)
            {
                if (i < stars && glyphs[i] == null)
                    glyphs[i] = MakeGlyph(i);
                if (glyphs[i] != null)
                    glyphs[i].gameObject.SetActive(i < stars);
            }
            // Without the game's star (no game scene) nothing was drawn: try again next time.
            shown = stars == 0 || glyphs[stars - 1] != null ? stars : -1;
        }

        private RectTransform MakeGlyph(int index)
        {
            RectTransform rect = StarGlyph.Create(transform, Size);
            if (rect == null)
                return null;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(CentreX, FirstCentreY - index * Step);
            return rect;
        }
    }
}
