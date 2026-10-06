using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The filter line in kitchen hover texts, appended once wherever the player looks at a kitchen: a plain cooking
    /// station through CookingStation.GetHoverText; the oven through its add-food switch (Switch.GetHoverText
    /// localizes the m_hoverText that UpdateCooking refreshes every second, while the oven's own GetHoverText returns
    /// ""); kitchen crafting stations through CraftingStation.GetHoverText while in use range (out of range it shows
    /// only "too far"). The line is added after localizing, so it is always current and never fills the game's small
    /// Localize cache with percentages. Where a ward denies the local player, the key hint is left out. The line (ward
    /// check, key hint, setting text) is kept for the kitchen in view and its filter setting, and rebuilt after 0.25 s
    /// or at once when either changes.
    /// </summary>
    public static class FilterHover
    {
        private const float RefreshSeconds = 0.25f;

        private static ZNetView lastView;
        private static int lastMinStars = -1;
        private static string lastLine = "";
        private static float refreshAt;

        private static string Line(ZNetView nview)
        {
            int minStars = KitchenFilter.MinStars(nview);
            float now = Time.time;
            if (ReferenceEquals(nview, lastView) && minStars == lastMinStars && now < refreshAt)
                return lastLine;
            bool access = PrivateArea.CheckAccess(nview.transform.position, 0f, flash: false);
            lastView = nview;
            lastMinStars = minStars;
            refreshAt = now + RefreshSeconds;
            lastLine = FilterText.HoverLine(minStars, access);
            return lastLine;
        }

        private static void Append(ZNetView nview, ref string text)
        {
            if (nview != null && !string.IsNullOrEmpty(text))
                text += Line(nview);
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
        private static class CookingStationHover
        {
            [HarmonyPostfix]
            private static void Postfix(CookingStation __instance, ref string __result) => Append(FilterStations.Of(__instance), ref __result);
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.GetHoverText))]
        private static class SwitchHover
        {
            [HarmonyPostfix]
            private static void Postfix(Switch __instance, ref string __result) => Append(FilterStations.Of(__instance), ref __result);
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
        private static class CraftingStationHover
        {
            [HarmonyPostfix]
            private static void Postfix(CraftingStation __instance, ref string __result)
            {
                Player player = Player.m_localPlayer;
                if (player != null && __instance.InUseDistance(player))
                    Append(FilterStations.Of(__instance), ref __result);
            }
        }
    }
}
