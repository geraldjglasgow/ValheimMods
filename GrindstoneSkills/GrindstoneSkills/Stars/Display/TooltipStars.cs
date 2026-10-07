using System;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A starred egg's tooltip: a line with its stars. The static ItemData.GetTooltip builds every item tooltip
    /// (inventory, containers, crafting; the instance overload calls it) before localization. The star line uses the
    /// game's own "$item_quality" label, which the game prints only for upgradable items and so never for an egg, and
    /// goes after the weight line, where the game puts quality; without a weight line it goes at the end. The tooltip of
    /// a second item appended to this one (m_appendToolTip) is a call of its own with appending set and is skipped.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class TooltipStars
    {
        private const string WeightKey = "\n$item_weight:";

        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, bool appending, ref string __result)
        {
            int stars = Stars.Get(item);
            if (appending || stars <= 0 || __result == null)
                return;
            __result = WithStarLine(__result, stars);
        }

        private static string WithStarLine(string text, int stars)
        {
            string line = "\n$item_quality: " + StarText.Tier(stars);
            int weight = text.IndexOf(WeightKey, StringComparison.Ordinal);
            int end = weight < 0 ? -1 : text.IndexOf('\n', weight + 1);
            return end < 0 ? text + line : text.Insert(end, line);
        }
    }
}
