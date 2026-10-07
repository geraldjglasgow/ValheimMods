using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// After a successful Player.EatFood the game has done one of three things, each starting the food at its burn
    /// time: added the dish as a new food (m_item is the eaten item), refreshed the food with the same shared name
    /// (only when CanEatAgain; the food keeps its old m_item and m_name), or replaced the most depleted food (m_item
    /// and m_name become the new dish's). This postfix finds that food, records the dish's stars for it (a refreshed
    /// food takes the new dish's stars), extends a starred dish's time, drops the key of a food it replaced and
    /// applies the new totals. FeastMaster's extra food slots eat through its own copy of the method, which skips
    /// the game's; a postfix runs either way. Eating is local to the eating player's client. The eaten item is still
    /// in the inventory here (Player.ConsumeItem removes it after EatFood), so its stars are still readable.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
    public static class FoodEaten
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result || item?.m_shared == null)
                return;
            HookGuard.Run("eat starred food", () => Record(__instance, item));
        }

        private static void Record(Player player, ItemDrop.ItemData item)
        {
            Player.Food food = Find(player, item);
            if (food == null)
                return;
            int stars = Stars.Get(item);
            FoodStars.Set(player, food.m_name, stars);
            Extend(food, item, stars);
            FoodStars.Prune(player);
            FoodTotals.Refresh(player);
        }

        /// <summary>
        /// A starred dish lasts longer. EatFood starts a food at m_time = m_foodBurnTime, and UpdateFood fades it with
        /// strength = clamp01(m_time / m_foodBurnTime) ^ 0.3, removing it at 0. Starting it at m_foodBurnTime * (1 +
        /// bonus) keeps it at full strength for the bonus time, since the curve clamps at 1, then it fades as usual.
        /// m_time is what Player.Save writes for each food, so the extra time survives a relog. CanEatAgain (below half
        /// the burn time) is unchanged, so the dish becomes re-eatable after its bonus time and half its burn time.
        /// </summary>
        private static void Extend(Player.Food food, ItemDrop.ItemData item, int stars)
        {
            float bonus = EatBonus.Duration(stars);
            float burnTime = item.m_shared.m_foodBurnTime;
            if (bonus > 0f && burnTime > 0f)
                food.m_time = Mathf.Max(food.m_time, burnTime * (1f + bonus));
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
