using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A starred dish lasts longer. Player.EatFood starts a food at m_time = m_foodBurnTime, and UpdateFood fades it
    /// with strength = clamp01(m_time / m_foodBurnTime) ^ 0.3, removing it at 0. Starting a starred dish at
    /// m_foodBurnTime * (1 + duration bonus) keeps it at full strength for the bonus time, since the curve clamps at
    /// 1, and then it fades as usual. m_time is what Player.Save writes for each food, so the extra time survives a
    /// relog. The game allows eating the same food again below half its burn time (CanEatAgain), so a starred dish
    /// also becomes re-eatable after its full-strength bonus time has run.
    /// </summary>
    public static class FoodDuration
    {
        /// <summary>Extends a food just eaten from a dish with these stars.</summary>
        public static void Extend(Player.Food food, ItemDrop.ItemData item, int stars)
        {
            float bonus = StarBonus.Duration(stars);
            float burnTime = item.m_shared.m_foodBurnTime;
            if (bonus <= 0f || burnTime <= 0f)
                return;
            food.m_time = Mathf.Max(food.m_time, burnTime * (1f + bonus));
        }
    }
}
