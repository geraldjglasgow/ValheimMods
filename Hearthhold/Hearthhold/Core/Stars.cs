using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// An item's quality stars, 0 to 3: plain, bronze, silver, gold. GrindstoneSkills keeps them in the item's quality
    /// and draws them (<see cref="GrindstoneLink"/>); only star items (<see cref="StarItems"/>) carry any.
    /// </summary>
    public static class Stars
    {
        public const int Max = 3;

        private static readonly string[] tierNames = { "Plain", "Bronze", "Silver", "Gold" };

        public static int Get(ItemDrop.ItemData item) => GrindstoneLink.GetStars(item);

        public static bool IsStarItem(ItemDrop.ItemData item) => GrindstoneLink.IsStarItem(item);

        public static void Set(ItemDrop.ItemData item, int stars) => GrindstoneLink.SetStars(item, Mathf.Clamp(stars, 0, Max));

        /// <summary>Sets the stars of an item lying in the world, owned here, and saves it to its ZDO at once.</summary>
        public static void SetOnDrop(ItemDrop drop, int stars)
        {
            if (drop == null || !IsStarItem(drop.m_itemData))
                return;
            Set(drop.m_itemData, stars);
            drop.SetQuality(drop.m_itemData.m_quality);
            if (drop.m_nview != null && drop.m_nview.IsValid() && drop.m_nview.IsOwner())
                drop.Save();
        }

        /// <summary>The quality an item with these stars has.</summary>
        public static int ToQuality(int stars) => Mathf.Clamp(stars, 0, Max) + 1;

        /// <summary>The stars as coloured glyphs, "" for none.</summary>
        public static string Text(int stars) => GrindstoneLink.StarText(stars);

        public static string TierName(int stars) => tierNames[Mathf.Clamp(stars, 0, Max)];
    }
}
