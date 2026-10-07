using System.Collections.Generic;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Which stations are kitchens, what they make and what they use. The game marks its own kitchens: a CookingStation
    /// whose m_skill is Cooking (cooking stations, oven, iron cooking station) and a CraftingStation whose
    /// m_craftingSkill is Cooking (cauldron, mead cauldron, prep table). Every fermenter is a kitchen. A modded kitchen
    /// that sets the same fields is found the same way. Filled by <see cref="Discovery"/>.
    /// </summary>
    public static class Kitchen
    {
        public const Skills.SkillType Skill = Skills.SkillType.Cooking;

        private static readonly HashSet<string> products = new HashSet<string>();
        private static readonly HashSet<string> ingredients = new HashSet<string>();
        private static readonly HashSet<string> fermented = new HashSet<string>();
        private static readonly HashSet<string> stationInputs = new HashSet<string>();
        private static readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public static bool IsKitchen(CookingStation station) => station != null && station.m_skill == Skill;

        public static bool IsKitchen(CraftingStation station) => station != null && station.m_craftingSkill == Skill;

        /// <summary>Whether a kitchen makes this item prefab (dishes, intermediates such as mead bases, meads).</summary>
        public static bool IsProduct(string prefabName) => prefabName != null && products.Contains(prefabName);

        /// <summary>Whether a kitchen uses this item prefab (a recipe requirement or a station's or barrel's input).</summary>
        public static bool IsIngredient(string prefabName) => prefabName != null && ingredients.Contains(prefabName);

        /// <summary>Whether a cooking station or fermenter takes this item prefab (raw meat, dough, mead bases...).</summary>
        public static bool IsStationInput(string prefabName) => prefabName != null && stationInputs.Contains(prefabName);

        /// <summary>Whether a fermenter makes this item prefab (meads and wines): what the aging cask ages.</summary>
        public static bool IsFermented(string prefabName) => prefabName != null && fermented.Contains(prefabName);

        /// <summary>
        /// An item's food value: health + stamina + eitr, or for an ingredient that is not eaten the value of what a
        /// station or barrel turns it into (raw meat as cooked meat, a mead base as its mead). 0 when unknown.
        /// </summary>
        public static float FoodValue(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
                return 0f;
            return values.TryGetValue(item.m_shared.m_name, out float value) ? value : Value(item.m_shared);
        }

        public static float Value(ItemDrop.ItemData.SharedData shared) => shared.m_food + shared.m_foodStamina + shared.m_foodEitr;

        internal static void AddProduct(ItemDrop item, bool fromFermenter)
        {
            if (item == null)
                return;
            products.Add(item.gameObject.name);
            if (fromFermenter)
                fermented.Add(item.gameObject.name);
        }

        internal static void AddIngredient(ItemDrop item)
        {
            if (item != null)
                ingredients.Add(item.gameObject.name);
        }

        /// <summary>Records that <paramref name="from"/> becomes <paramref name="to"/>, for the food value of ingredients.</summary>
        internal static void AddConversion(ItemDrop from, ItemDrop to)
        {
            if (from?.m_itemData?.m_shared == null || to?.m_itemData?.m_shared == null)
                return;
            stationInputs.Add(from.gameObject.name);
            ItemDrop.ItemData.SharedData shared = from.m_itemData.m_shared;
            if (Value(shared) <= 0f && !values.ContainsKey(shared.m_name))
                values[shared.m_name] = Value(to.m_itemData.m_shared);
        }

        /// <summary>The ItemDrop of an item prefab by name, or null.</summary>
        public static ItemDrop ItemPrefab(string prefabName)
        {
            GameObject prefab = ObjectDB.instance == null || string.IsNullOrEmpty(prefabName) ? null : ObjectDB.instance.GetItemPrefab(prefabName);
            return prefab == null ? null : prefab.GetComponent<ItemDrop>();
        }

        internal static int ProductCount => products.Count;
    }
}
