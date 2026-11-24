using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Hides the game's crafting panel while the Spellbook is open in its place, and puts it back exactly as it was when the
    /// Spellbook closes (its button again, the inventory closed by any key, Esc included, no wand or book left in hand,
    /// the switch off). It is hidden through a CanvasGroup on the panel (the panel's own, or one added for it and removed
    /// again): nothing of it drawn, nothing of it taking a click, while the game keeps running it as usual (its
    /// UIGroupHandler only ever sets a group's interactable, and only a group it found at its start). The panel's own
    /// parts and objects are never switched off, so whatever the game or another mod shows on it comes back as it was.
    /// </summary>
    public static class SpellbookCover
    {
        private static CanvasGroup group;
        private static bool added;
        private static float alpha;
        private static bool raycasts;

        /// <summary>Hides the crafting panel (nothing while it is already hidden).</summary>
        public static void Cover(RectTransform crafting)
        {
            if (crafting == null || group != null)
                return;
            group = crafting.GetComponent<CanvasGroup>();
            added = group == null;
            if (added)
                group = crafting.gameObject.AddComponent<CanvasGroup>();
            alpha = group.alpha;
            raycasts = group.blocksRaycasts;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        /// <summary>Shows the crafting panel again as it was; nothing when it is gone with its scene.</summary>
        public static void Restore()
        {
            if (group != null && added)
                Object.DestroyImmediate(group);
            else if (group != null)
                (group.alpha, group.blocksRaycasts) = (alpha, raycasts);
            group = null;
        }
    }
}
