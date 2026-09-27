using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Which stacks a recipe takes. Inventory.RemoveItem(name, amount, itemQuality, worldLevelBased) walks the
    /// inventory in list order and takes from every stack with that name (and quality, unless it is -1) that is not
    /// below the world level. For an item that carries stars, asked for at any quality, the prefix takes the same
    /// amount in the local player's Ingredient Order instead: fewest stars first (the default, keeps the best food) or
    /// most stars first. While a kitchen craft records (<see cref="CraftRecord"/>) it performs every removal by name,
    /// in the game's order for items without stars, and notes what it took. Then, as the game does, it drops emptied
    /// stacks and raises the inventory's change event.
    /// The order applies to every inventory, not only the local player's: it only chooses which units of the same
    /// item go, never how many, so it is harmless anywhere, and it follows a mod that pays a recipe from another
    /// inventory the game's way. It runs after other mods' prefixes (low priority), so OpenKeep first takes the
    /// shortfall from chests and then this takes the inventory's part.
    /// </summary>
    public static class IngredientTakeOrder
    {
        public static bool HighestFirst => DisplaySettings.Order != null && DisplaySettings.Order.Value == IngredientOrder.HighestStarsFirst;

        /// <summary>The qualities of an item that carries stars, in the order the player wants them used.</summary>
        public static IEnumerable<int> Qualities()
        {
            for (int i = 0; i <= Stars.Max; i++)
                yield return Stars.ToQuality(HighestFirst ? Stars.Max - i : i);
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        private static class RemoveByName
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Low)]
            private static bool Prefix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                bool stars = Kitchen.IsKitchenName(name);
                if (!CraftRecord.Recording && !(stars && itemQuality < 0))
                    return true;
                Take(__instance, Stacks(__instance, name, itemQuality, worldLevelBased, stars), amount);
                return false;
            }
        }

        /// <summary>The stacks the game would take from, in the order to take them.</summary>
        private static List<ItemDrop.ItemData> Stacks(Inventory inventory, string name, int quality, bool worldLevelBased, bool stars)
        {
            IEnumerable<ItemDrop.ItemData> stacks = inventory.m_inventory.Where(item => item.m_shared.m_name == name
                && (quality < 0 || item.m_quality == quality)
                && (!worldLevelBased || item.m_worldLevel >= Game.m_worldLevel));
            if (stars)
                stacks = HighestFirst ? stacks.OrderByDescending(Stars.Get) : stacks.OrderBy(Stars.Get);
            return stacks.ToList();
        }

        private static void Take(Inventory inventory, List<ItemDrop.ItemData> stacks, int amount)
        {
            foreach (ItemDrop.ItemData stack in stacks)
            {
                if (amount <= 0)
                    break;
                int take = Mathf.Min(stack.m_stack, amount);
                CraftRecord.Note(stack, take);
                stack.m_stack -= take;
                amount -= take;
            }
            inventory.m_inventory.RemoveAll(item => item.m_stack <= 0);
            inventory.Changed();
        }
    }
}
