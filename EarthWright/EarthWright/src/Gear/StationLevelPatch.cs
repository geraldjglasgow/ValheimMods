using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// The crafting station level a terrain tool's level needs, from "Upgrade Station Levels". The game asks for one
    /// station level per tool level, so a hoe at level 6 would need a level 6 workbench, which does not exist; the list
    /// lets the server decide. Only the hoe, cultivator and shovel recipes are touched, and nothing while EarthWright is off.
    /// </summary>
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel))]
    public static class StationLevelPatch
    {
        private static CachedNumbers levels;

        [HarmonyPostfix]
        public static void Postfix(Recipe __instance, int quality, ref int __result)
        {
            if (GearSettings.StationLevels == null || !ToolRecipes.IsToolRecipe(__instance) || !GeneralSettings.Enabled.Value)
                return;
            if (levels == null)
                levels = new CachedNumbers(GearSettings.StationLevels);
            float? level = SettingLists.ForLevel(levels.Current, quality);
            if (level.HasValue)
                __result = Mathf.Max(1, Mathf.RoundToInt(level.Value));
        }
    }
}
