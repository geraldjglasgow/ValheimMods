using HarmonyLib;
using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// A giant crop's hover shows "Giant" before its name. Pickable.GetHoverText is the localized name, then a line with
    /// the pick key, or empty once picked. Read from the crop's ZDO on the viewing client (<see cref="CropKeys"/>). Only
    /// the name's line goes into the word, so a language that puts "Giant" after the name keeps the pick key below it.
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    public static class CropHover
    {
        [HarmonyPostfix]
        private static void Postfix(Pickable __instance, ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && FarmSkill.Active && CropKeys.Giant(__instance.m_nview))
                __result = Giant(__result);
        }

        private static string Giant(string hover)
        {
            int end = hover.IndexOf('\n');
            if (end < 0)
                return Lang.Format("gs_farm_giant_name", hover);
            return Lang.Format("gs_farm_giant_name", hover.Substring(0, end)) + hover.Substring(end);
        }
    }
}
