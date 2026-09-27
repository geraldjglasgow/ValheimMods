using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which stations are kitchens and which items carry stars. The game marks its own kitchens: a CookingStation
    /// whose m_skill is Cooking (cooking stations, oven) and a CraftingStation whose m_craftingSkill is Cooking
    /// (cauldron, mead cauldron, prep table). Every fermenter is a kitchen. Kitchen items are what those make:
    /// cooking-station and fermenter outputs and kitchen recipe outputs, intermediates (mead bases, dough, unbaked
    /// pies) included. Feasts (items that place a piece) and unstackable items never carry stars. A modded kitchen
    /// that sets the same fields is found the same way. Filled by <see cref="KitchenDiscovery"/>.
    /// </summary>
    public static class Kitchen
    {
        private static readonly HashSet<string> itemNames = new HashSet<string>();
        private static readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public static bool IsKitchen(CookingStation station) => station != null && station.m_skill == CookLevel.Skill;

        public static bool IsKitchen(CraftingStation station) => station != null && station.m_craftingSkill == CookLevel.Skill;

        /// <summary>Whether the item can carry stars. Keyed by the shared name, which is also what stacks compare.</summary>
        public static bool IsKitchenItem(ItemDrop.ItemData item) => item?.m_shared != null && itemNames.Contains(item.m_shared.m_name);

        /// <summary>Whether items with this shared name (m_shared.m_name, e.g. "$item_cookedmeat") can carry stars.</summary>
        public static bool IsKitchenName(string sharedName) => sharedName != null && itemNames.Contains(sharedName);

        /// <summary>Whether the item prefab (by prefab name) can carry stars.</summary>
        public static bool IsKitchenItem(string prefabName)
        {
            ItemDrop drop = ItemPrefab(prefabName);
            return drop != null && IsKitchenItem(drop.m_itemData);
        }

        /// <summary>
        /// The dish's food value for experience: health + stamina + eitr, or for an intermediate the value of what it
        /// becomes on a station or in a fermenter. 0 when unknown.
        /// </summary>
        public static float FoodValue(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
                return 0f;
            return values.TryGetValue(item.m_shared.m_name, out float value) ? value : Value(item.m_shared);
        }

        /// <summary>The ItemDrop of an item prefab by name, or null.</summary>
        public static ItemDrop ItemPrefab(string prefabName)
        {
            GameObject prefab = ObjectDB.instance == null || string.IsNullOrEmpty(prefabName) ? null : ObjectDB.instance.GetItemPrefab(prefabName);
            return prefab == null ? null : prefab.GetComponent<ItemDrop>();
        }

        internal static float Value(ItemDrop.ItemData.SharedData shared) => shared.m_food + shared.m_foodStamina + shared.m_foodEitr;

        /// <summary>
        /// Adds an item that can carry stars. Feasts and unstackable items are refused. Quality also scales an item's
        /// size and weight when its shared data sets m_scaleByQuality / m_scaleWeightByQuality; those are cleared so a
        /// starred dish looks and weighs the same (without GrindstoneSkills a kitchen item is always quality 1, where both
        /// factors do nothing, so this changes nothing for plain dishes).
        /// </summary>
        internal static void AddItem(ItemDrop drop)
        {
            if (drop == null || drop.m_itemData?.m_shared == null || drop.GetComponent<Piece>() != null || drop.GetComponent<Feast>() != null)
                return;
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            if (shared.m_maxStackSize <= 1)
                return;
            itemNames.Add(shared.m_name);
            shared.m_scaleByQuality = 0f;
            shared.m_scaleWeightByQuality = 0f;
        }

        /// <summary>
        /// Lets another module's item carry stars the way dishes do (crops, seeds, forage, eggs): it joins the items every
        /// star feature keys on (stacking apart, icons, tooltips, recipe counting, the ingredient bonus, eating). Same
        /// rules and refusals as the kitchen's own items. Idempotent, so it can be called whenever the prefab exists.
        /// </summary>
        public static void AddStarItem(ItemDrop drop) => AddItem(drop);

        /// <summary>Records that <paramref name="from"/> becomes <paramref name="to"/>, for the food value of intermediates.</summary>
        internal static void AddConversion(ItemDrop from, ItemDrop to)
        {
            if (from?.m_itemData?.m_shared == null || to?.m_itemData?.m_shared == null)
                return;
            ItemDrop.ItemData.SharedData shared = from.m_itemData.m_shared;
            if (Value(shared) <= 0f && !values.ContainsKey(shared.m_name))
                values[shared.m_name] = Value(to.m_itemData.m_shared);
        }

        internal static int Count => itemNames.Count;
    }
}
