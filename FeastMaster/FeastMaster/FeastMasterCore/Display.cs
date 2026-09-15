using System;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>Hides the numbers on the health, stamina and eitr bars for this client when the display settings say so.</summary>
    public static class HudNumbers
    {
        public static void Show(TMP_Text text, bool show)
        {
            if (text == null)
                return;
            GameObject go = text.gameObject;
            if (go.activeSelf != show)
                go.SetActive(show);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateHealth))]
    public static class HideHealthNumberPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance) => HudNumbers.Show(__instance.m_healthText, !Settings.HideHealthNumber.Value);
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStamina))]
    public static class HideStaminaNumberPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance) => HudNumbers.Show(__instance.m_staminaText, !Settings.HideStaminaNumber.Value);
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateEitr))]
    public static class HideEitrNumberPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance) => HudNumbers.Show(__instance.m_eitrText, !Settings.HideEitrNumber.Value);
    }

    /// <summary>
    /// Food Timers: the game always shows the remaining time under each active food icon (Hud.UpdateFood enables
    /// m_foodTime[i] for every active food), so Vanilla and Always both show it and Never hides it.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
    public static class FoodTimersPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Hud __instance, Player player)
        {
            FoodTimers mode = Settings.FoodTimersMode.Value;
            if (mode == FoodTimers.Vanilla || __instance.m_foodTime == null)
                return;
            int active = Mathf.Min(player.GetFoods().Count, __instance.m_foodTime.Length);
            for (int i = 0; i < active; i++)
                HudNumbers.Show(__instance.m_foodTime[i], mode == FoodTimers.Always);
        }
    }

    /// <summary>
    /// Adds "Vigor: +X% stamina regen" and "Eitr Vigor: +X% eitr regen" to a food's tooltip after the game's regen
    /// line (or the duration line when the food has no regen), in the orange the game uses for its own regen line.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class VigorTooltipPatch
    {
        public const string VigorKey = "fm_vigor";
        public const string VigorUnitKey = "fm_vigor_regen";
        public const string EitrVigorKey = "fm_eitr_vigor";
        public const string EitrVigorUnitKey = "fm_eitr_vigor_regen";

        [HarmonyPostfix]
        public static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (item == null || !IsFood(item.m_shared))
                return;
            string lines = Line(VigorKey, VigorUnitKey, ItemValues.VigorOf(item))
                + Line(EitrVigorKey, EitrVigorUnitKey, ItemValues.EitrVigorOf(item));
            if (lines.Length > 0)
                __result = __result.Insert(InsertAt(__result), lines);
        }

        private static string Line(string key, string unitKey, float percent)
        {
            if (percent == 0f)
                return "";
            return $"\n${key}: <color=orange>{percent.ToString("+0.#;-0.#")}% ${unitKey}</color>";
        }

        private static bool IsFood(ItemDrop.ItemData.SharedData shared)
        {
            return shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f;
        }

        /// <summary>End of the regen line, else end of the duration line, else end of the text.</summary>
        private static int InsertAt(string tooltip)
        {
            int start = tooltip.IndexOf("$item_food_regen", StringComparison.Ordinal);
            if (start < 0)
                start = tooltip.IndexOf("$item_food_duration", StringComparison.Ordinal);
            if (start < 0)
                return tooltip.Length;
            int end = tooltip.IndexOf('\n', start);
            return end < 0 ? tooltip.Length : end;
        }
    }

    /// <summary>
    /// Registers the English words behind the tooltip tokens. The game clears its dictionary on every language
    /// setup, so the words are added again after each one.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class VigorWordsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Localization __instance)
        {
            __instance.AddWord(VigorTooltipPatch.VigorKey, "Vigor");
            __instance.AddWord(VigorTooltipPatch.VigorUnitKey, "stamina regen");
            __instance.AddWord(VigorTooltipPatch.EitrVigorKey, "Eitr Vigor");
            __instance.AddWord(VigorTooltipPatch.EitrVigorUnitKey, "eitr regen");
        }
    }
}
