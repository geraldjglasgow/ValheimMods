using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// After a successful Player.EatFood the game has done one of three things, each starting the food at its burn
    /// time: added the dish as a new food (m_item is the eaten item), refreshed the food with the same shared name
    /// (only when CanEatAgain; the food keeps its old m_item and m_name), or replaced the most depleted food (m_item
    /// and m_name become the new dish's). This postfix finds that food, records the dish's stars for it (a refreshed
    /// food takes the new dish's stars), extends a starred dish's time, drops the key of a food it replaced and
    /// applies the new totals. FeastMaster's extra food slots eat through its own copy of the method, which skips
    /// the game's; a postfix runs either way. Eating is local to the eating player's client.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
    public static class FoodEaten
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result || item?.m_shared == null)
                return;
            Player.Food food = Find(__instance, item);
            if (food == null)
                return;
            int stars = Stars.Get(item);
            FoodStars.Set(__instance, food.m_name, stars);
            FoodDuration.Extend(food, item, stars);
            FoodStars.Prune(__instance);
            FoodTotals.Refresh(__instance);
        }

        /// <summary>The food holding the eaten item, else the one with its shared name (a refreshed food).</summary>
        private static Player.Food Find(Player player, ItemDrop.ItemData item)
        {
            foreach (Player.Food food in player.m_foods)
            {
                if (food.m_item == item)
                    return food;
            }
            foreach (Player.Food food in player.m_foods)
            {
                if (food.m_item?.m_shared?.m_name == item.m_shared.m_name)
                    return food;
            }
            return null;
        }
    }
}
