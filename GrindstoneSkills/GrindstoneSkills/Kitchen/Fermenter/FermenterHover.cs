using System;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The barrel's hover text shows the base's stars. The game's GetHoverText names the content (the base's shared
    /// name, localized) while fermenting and while a batch waits to be tapped; when that base has stars, they are put
    /// right after the name. Without ward access the game leaves the content out, and so does this.
    /// </summary>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
    internal static class FermenterHover
    {
        private const char Star = '★';

        [HarmonyPostfix]
        private static void Postfix(Fermenter __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || __instance.m_nview == null || __instance.GetContent() == 0)
                return;
            int stars = FermenterBase.GetStars(__instance.m_nview.GetZDO());
            if (stars <= 0 || Localization.instance == null)
                return;
            string name = Localization.instance.Localize(__instance.GetContentName());
            int at = string.IsNullOrEmpty(name) ? -1 : __result.IndexOf(name, StringComparison.Ordinal);
            if (at >= 0)
                __result = __result.Insert(at + name.Length, StarText(stars));
        }

        private static string StarText(int stars) => " <color=orange>" + new string(Star, stars) + "</color>";
    }
}
