using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Maker's Mark: a weapon, armour piece, shield or tool (an equipable item that wears) crafted or upgraded by the local
    /// player at a station that trains Crafting carries the maker's Crafting level in its custom data
    /// (<see cref="Keys.MakerLevel"/>, invariant culture), and its max durability is up to 25% higher at level 100, linear
    /// from 0, on every machine (<see cref="MarkDurability"/>, read through <see cref="MarkReader"/>). The game saves
    /// custom data with the item in inventories, containers and drops, so the mark follows the item to any player. An
    /// upgrade makes a new item in the game (InventoryGui.DoCrafting removes the old one and adds a fresh copy), so the
    /// upgrader's level marks it again, and an upgrade made while the module is off leaves it unmarked, as the game would.
    /// Stamped on the crafter's client by <see cref="StationCraft"/>.
    /// </summary>
    public static class MakersMark
    {
        /// <summary>Extra max durability at Crafting 100.</summary>
        private const float DurabilityAt100 = 0.25f;

        /// <summary>Whether crafting the item can mark it: it is equipable and wears.</summary>
        public static bool Qualifies(ItemDrop.ItemData item) =>
            item?.m_shared != null && item.m_shared.m_useDurability && item.IsEquipable();

        /// <summary>The extra max durability a maker of this level gives, 0 to 0.25; 0 for a level that is not a number.</summary>
        public static float Share(float level) => level > 0f ? DurabilityAt100 * Mathf.Clamp01(level / 100f) : 0f;

        /// <summary>
        /// Marks a new item with the maker's level and fills it to its new max durability. The custom data goes into a
        /// new dictionary, so any reader that keeps a parse per dictionary (this mod's, another mod's) reads it again.
        /// </summary>
        public static void Stamp(ItemDrop.ItemData item, float level)
        {
            Dictionary<string, string> data = item.m_customData != null
                ? new Dictionary<string, string>(item.m_customData)
                : new Dictionary<string, string>();
            data[Keys.MakerLevel] = level.ToString("0.##", CultureInfo.InvariantCulture);
            item.m_customData = data;
            item.m_durability = item.GetMaxDurability();
        }

        /// <summary>The tooltip line for a share, before localization: "Maker's Mark: +14.3% durability".</summary>
        public static string LineFor(float share) =>
            "\nMaker's Mark: <color=orange>+" + (share * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "% durability</color>";
    }
}
