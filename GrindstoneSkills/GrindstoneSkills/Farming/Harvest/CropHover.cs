using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A ripe crop's hover shows its stars after the name, and "Giant" before it. Pickable.GetHoverText is the localized
    /// name, then a line with the pick key, or empty once picked; the stars go at the end of the first line. Read from
    /// the crop's ZDO on the viewing client (<see cref="CropKeys"/>).
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    public static class CropHover
    {
        [HarmonyPostfix]
        private static void Postfix(Pickable __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || !FarmSkill.Active)
                return;
            int stars = CropKeys.Stars(__instance.m_nview);
            bool giant = CropKeys.Giant(__instance.m_nview);
            if (stars > 0 || giant)
                __result = Decorate(__result, stars, giant);
        }

        private static string Decorate(string text, int stars, bool giant)
        {
            int line = text.IndexOf('\n');
            string name = line < 0 ? text : text.Substring(0, line);
            string rest = line < 0 ? "" : text.Substring(line);
            return (giant ? "Giant " : "") + name + (stars > 0 ? " " + StarText.Colored(stars) : "") + rest;
        }
    }
}
