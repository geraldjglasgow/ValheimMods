using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A giant crop's hover shows "Giant" before its name. Pickable.GetHoverText is the localized name, then a line with
    /// the pick key, or empty once picked. Read from the crop's ZDO on the viewing client (<see cref="CropKeys"/>).
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    public static class CropHover
    {
        [HarmonyPostfix]
        private static void Postfix(Pickable __instance, ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && FarmSkill.Active && CropKeys.Giant(__instance.m_nview))
                __result = "Giant " + __result;
        }
    }
}
