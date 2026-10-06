using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A dish's stars live in the item's own quality field: quality 1 is 0 stars (every vanilla dish), 4 is 3 stars.
    /// Kitchen items have a maximum quality of 1, so the game never offers to upgrade them and hides the quality in
    /// tooltips. Quality is saved with the item in inventories and in the ZDO of an item on the ground, and the
    /// game's own free-stack search already keeps different qualities apart.
    /// </summary>
    public static class Stars
    {
        public const int Max = 3;

        /// <summary>The item's stars; 0 for anything that is not a kitchen item.</summary>
        public static int Get(ItemDrop.ItemData item)
        {
            // Quality first: nearly every item has quality 1 (0 stars), which needs no kitchen lookup.
            if (item == null || item.m_quality <= 1 || !Kitchen.IsKitchenItem(item))
                return 0;
            return Mathf.Clamp(item.m_quality - 1, 0, Max);
        }

        /// <summary>Gives the item the stars, clamped to 0..3. Does nothing to items that are not kitchen items.</summary>
        public static void Set(ItemDrop.ItemData item, int stars)
        {
            if (item != null && Kitchen.IsKitchenItem(item))
                item.m_quality = ToQuality(stars);
        }

        public static int ToQuality(int stars) => Mathf.Clamp(stars, 0, Max) + 1;

        public static int FromQuality(int quality) => Mathf.Clamp(quality - 1, 0, Max);
    }
}
