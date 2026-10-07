using UnityEngine;

namespace GrindstoneSkills.Api
{
    /// <summary>
    /// Item stars for other mods, read by reflection (Hearthhold does): plain game types only. A star item keeps its
    /// stars in its quality (quality 1 = no stars, 4 = three) and gets everything GrindstoneSkills does for eggs:
    /// stacks apart by stars, counts for recipes at any stars, and shows its stars on icons, tooltips and hover text in
    /// bronze, silver and gold. Main thread only. Later versions only add endpoints.
    /// </summary>
    public static class StarsApi
    {
        public static int GetApiVersion() => 1;

        public static int GetMaxStars() => Stars.Max;

        /// <summary>Makes an item prefab a star item. Unstackable items are refused. Idempotent; nothing is ever removed.</summary>
        public static void AddStarItem(GameObject itemPrefab) => Stars.Add(itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null);

        public static bool IsStarItem(ItemDrop.ItemData item) => Stars.IsStarItem(item);

        /// <summary>The item's stars, 0 to <see cref="GetMaxStars"/>; 0 for anything that is not a star item.</summary>
        public static int GetStars(ItemDrop.ItemData item) => Stars.Get(item);

        /// <summary>Sets a star item's stars (its quality); does nothing for any other item. An item in the world also needs ItemDrop.SetQuality and Save.</summary>
        public static void SetStars(ItemDrop.ItemData item, int stars)
        {
            if (Stars.IsStarItem(item))
                item.m_quality = Mathf.Clamp(stars, 0, Stars.Max) + 1;
        }

        /// <summary>The stars as coloured rich text glyphs (bronze, silver, gold), "" for none.</summary>
        public static string GetStarText(int stars) => StarText.Tier(stars);
    }
}
