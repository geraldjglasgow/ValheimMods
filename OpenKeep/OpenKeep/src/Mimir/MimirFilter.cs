using System;
using OpenKeep.Core;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The quick filters of Mímir's Chest (user, 2026-10-07: "little quick filter icons for like food, equipment, wood"):
    /// one category at a time, combined with the search text. Decided from the item's type and prefab name alone, so the
    /// same on every machine and for modded items. Food: eaten for stats (and fish). Meads: other consumables. Equipment:
    /// anything worn or held (weapons, shields, armour, tools, torches, ammo, utility, trinkets). Wood: materials named
    /// wood, logs and bark. Metal: ores, scrap, bars and nails. Trophies. Other: every other material and item.
    /// </summary>
    public enum MimirFilter
    {
        All,
        Food,
        Meads,
        Equipment,
        Wood,
        Metal,
        Trophies,
        Other,
    }

    public static class MimirFilters
    {
        public static readonly MimirFilter[] Buttons =
        {
            MimirFilter.Food, MimirFilter.Meads, MimirFilter.Equipment, MimirFilter.Wood, MimirFilter.Metal,
            MimirFilter.Trophies, MimirFilter.Other,
        };

        private static readonly string[] WoodWords = { "wood", "log", "bark" };
        private static readonly string[] MetalStarts = { "copper", "tin", "bronze", "iron", "silver", "blackmetal", "flametal", "metal" };

        public static bool Matches(MimirFilter filter, ItemDrop.ItemData item)
        {
            if (filter == MimirFilter.All)
                return true;
            return item?.m_shared != null && Of(item) == filter;
        }

        public static MimirFilter Of(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            ItemDrop.ItemData.ItemType type = shared.m_itemType;
            if (type == ItemDrop.ItemData.ItemType.Consumable)
                return shared.m_food + shared.m_foodStamina + shared.m_foodEitr > 0f ? MimirFilter.Food : MimirFilter.Meads;
            if (type == ItemDrop.ItemData.ItemType.Fish)
                return MimirFilter.Food;
            if (type == ItemDrop.ItemData.ItemType.Trophy)
                return MimirFilter.Trophies;
            if (item.IsEquipable() || type == ItemDrop.ItemData.ItemType.Ammo || type == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
                return MimirFilter.Equipment;
            if (type == ItemDrop.ItemData.ItemType.Material)
                return MaterialKind(ItemNames.PrefabName(item).ToLowerInvariant());
            return MimirFilter.Other;
        }

        private static MimirFilter MaterialKind(string prefab)
        {
            if (Contains(prefab, WoodWords))
                return MimirFilter.Wood;
            return IsMetal(prefab) ? MimirFilter.Metal : MimirFilter.Other;
        }

        // Starts with a metal (TinOre, but not Chitin), or an ore or scrap (FlametalOreNew, but not SurtlingCore).
        private static bool IsMetal(string prefab) =>
            Array.Exists(MetalStarts, metal => prefab.StartsWith(metal))
            || ((prefab.Contains("ore") && !prefab.Contains("core")) || prefab.Contains("scrap"));

        private static bool Contains(string text, string[] words) =>
            Array.Exists(words, word => text.Contains(word));

        /// <summary>
        /// A stack with quality stars: quality above 1 on a stackable item that cannot be upgraded (GrindstoneSkills'
        /// eggs, Hearthhold's food). No reference to either mod; this is how both store stars.
        /// </summary>
        public static bool IsStarred(ItemDrop.ItemData item) =>
            item?.m_shared != null && item.m_quality > 1 && item.m_shared.m_maxQuality <= 1 && item.m_shared.m_maxStackSize > 1;

        /// <summary>The item whose icon the button shows.</summary>
        public static string IconItem(MimirFilter filter)
        {
            switch (filter)
            {
                case MimirFilter.Food: return "CookedMeat";
                case MimirFilter.Meads: return "MeadHealthMinor";
                case MimirFilter.Equipment: return "SwordBronze";
                case MimirFilter.Wood: return "Wood";
                case MimirFilter.Metal: return "Iron";
                case MimirFilter.Trophies: return "TrophyDeer";
                default: return "Resin";
            }
        }

        /// <summary>The button's tooltip word, "$ok_mimir_filter_food" and so on (<see cref="MimirModule"/>).</summary>
        public static string Word(MimirFilter filter) => "$ok_mimir_filter_" + filter.ToString().ToLowerInvariant();
    }
}
