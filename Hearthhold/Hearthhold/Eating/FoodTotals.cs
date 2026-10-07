using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// The star bonus to max health, stamina and eitr. Player.GetTotalFoodValue sums the base values and every active
    /// food's current m_health, m_stamina and m_eitr; UpdateFood recomputes those from the item's base values and the
    /// fading curve every second, and FeastMaster recomputes them again in its prefix for its degradation settings.
    /// This postfix adds each starred food's share of what was summed, so the bonus follows the curve and whatever
    /// another mod set per food, and the per-food values themselves are never written. UpdateFood is the method's only
    /// caller and passes the totals to SetMaxHealth, SetMaxStamina and SetMaxEitr, which the HUD bars, the save and
    /// every other reader of max health, stamina and eitr use. Without custom data it costs one count test.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class FoodTotals
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            if (__instance.m_customData.Count == 0)
                return;
            foreach (Player.Food food in __instance.m_foods)
            {
                float bonus = EatBonus.Food(FoodStars.Get(__instance, food));
                ItemDrop.ItemData.SharedData shared = food.m_item?.m_shared;
                if (bonus <= 0f || shared == null)
                    continue;
                hp += food.m_health * EatBonus.RoundedShare(shared.m_food, bonus);
                stamina += food.m_stamina * EatBonus.RoundedShare(shared.m_foodStamina, bonus);
                eitr += food.m_eitr * EatBonus.RoundedShare(shared.m_foodEitr, bonus);
            }
        }

        /// <summary>
        /// Sets the max values from the totals now, as UpdateFood does after each food tick, so a dish's stars show
        /// the moment it is eaten rather than at the next tick. The bars flash when a value rises, as when eating.
        /// </summary>
        public static void Refresh(Player player)
        {
            player.GetTotalFoodValue(out float hp, out float stamina, out float eitr);
            player.SetMaxHealth(hp, flashBar: true);
            player.SetMaxStamina(stamina, flashBar: true);
            player.SetMaxEitr(eitr, flashBar: true);
        }
    }
}
