using HarmonyLib;

namespace Broadside
{
    /// <summary>
    /// A hammock's hover is the game's bed's, named for a hammock: "Hammock", "Gerald's Hammock", "Unclaimed hammock".
    /// <c>Bed.GetHoverName</c> and <c>Bed.GetHoverText</c> postfixes, only for a bed with <see cref="KwHammock"/> on it; the
    /// game's words are replaced after the game has put them in its language. Local, nothing sent.
    /// </summary>
    [HarmonyPatch]
    public static class KwHammockHover
    {
        [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverName)), HarmonyPostfix]
        private static void Name(Bed __instance, ref string __result)
        {
            if (__instance.TryGetComponent(out KwHammock _))
                __result = KwHammocks.Label;
        }

        [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverText)), HarmonyPostfix]
        private static void Text(Bed __instance, ref string __result)
        {
            if (__result == null || Localization.instance == null || !__instance.TryGetComponent(out KwHammock _))
                return;
            Localization words = Localization.instance;
            __result = __result.Replace(words.Localize("$piece_bed_unclaimed"), "Unclaimed hammock")
                .Replace("'s " + words.Localize("$piece_bed"), "'s " + KwHammocks.Label);
        }
    }
}
