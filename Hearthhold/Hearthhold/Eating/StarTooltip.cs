using System.Text;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// A starred food's or mead's tooltip: one line with what its stars do, with the boosted numbers, at the end, e.g.
    /// "Silver: +20% health and stamina (60 / 24), lasts 20% longer (36m)". The static ItemData.GetTooltip builds
    /// every item tooltip (inventory, containers, crafting; the instance overload calls it) before localization;
    /// GrindstoneSkills adds the stars line itself, so only the bonus is written here. The tooltip of a second item
    /// appended to this one (m_appendToolTip) is a call of its own with appending set and is skipped. An item without
    /// stars costs one star read and allocates nothing.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class StarTooltip
    {
        private static readonly StringBuilder text = new StringBuilder();

        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, bool appending, ref string __result)
        {
            if (appending || __result == null || item?.m_shared == null)
                return;
            int stars = Stars.Get(item);
            if (stars <= 0)
                return;
            text.Clear();
            FoodLine(item.m_shared, stars);
            MeadLine(item.m_shared.m_consumeStatusEffect, stars);
            if (text.Length > 0)
                __result += text.ToString();
        }

        private static void FoodLine(ItemDrop.ItemData.SharedData shared, int stars)
        {
            if (Kitchen.Value(shared) <= 0f)
                return;
            float bonus = EatBonus.Food(stars);
            float duration = EatBonus.Duration(stars);
            Label(stars).Append('+').Append(EatBonus.Percent(bonus)).Append("% ");
            StatLine.Values(text, shared.m_food, shared.m_foodStamina, shared.m_foodEitr, 1f + bonus);
            text.Append(", lasts ").Append(EatBonus.Percent(duration)).Append("% longer (")
                .Append(ItemDrop.ItemData.GetDurationString(shared.m_foodBurnTime * (1f + duration))).Append(')');
        }

        private static void MeadLine(StatusEffect effect, int stars)
        {
            MeadKind kind = MeadKinds.Of(effect);
            if (kind == MeadKind.Restore)
                Restore((SE_Stats)effect, EatBonus.Restore(stars), stars);
            else if (kind == MeadKind.Lasting)
                Lasting(effect.m_ttl, EatBonus.Lasting(stars), stars);
        }

        private static void Restore(SE_Stats stats, float bonus, int stars)
        {
            Label(stars).Append("restores ").Append(EatBonus.Percent(bonus)).Append("% more ");
            float stamina = stats.m_staminaUpFront + (stats.m_staminaOverTimeIsFraction ? 0f : stats.m_staminaOverTime);
            float health = stats.m_healthUpFront + stats.m_healthOverTime;
            StatLine.Values(text, health, stamina, stats.m_eitrUpFront + stats.m_eitrOverTime, 1f + bonus);
        }

        private static void Lasting(float ttl, float bonus, int stars)
        {
            Label(stars).Append("lasts ").Append(EatBonus.Percent(bonus)).Append("% longer (")
                .Append(ItemDrop.ItemData.GetDurationString(ttl * (1f + bonus))).Append(')');
        }

        private static StringBuilder Label(int stars) =>
            text.Append("\n<color=orange>").Append(Stars.TierName(stars)).Append("</color>: ");
    }
}
