using System;
using HarmonyLib;
using PatchGuard;
using Translations;

namespace Hearthhold
{
    /// <summary>
    /// The trader's hover text gets one more line, the local player's friendship with it ("Friendship 3/10, 3% off").
    /// Hover text runs every frame: the joined text is kept per trader and reused while the game's own text, the heart
    /// count and the words (<c>Lang.Version</c>) are unchanged. No line while the switch is off. The heart is spelled
    /// out, not drawn as a glyph, since the game's fonts are not known to carry one.
    /// </summary>
    public static class TraderHover
    {
        [HarmonyPatch(typeof(Trader), nameof(Trader.GetHoverText))]
        private static class Hover
        {
            [HarmonyPostfix]
            private static void Postfix(Trader __instance, ref string __result)
            {
                if (!Friendship.On || Player.m_localPlayer == null || string.IsNullOrEmpty(__result))
                    return;
                try
                {
                    __result = WithLine(__instance, __result);
                }
                catch (Exception exception)
                {
                    Guard.Report(exception, "trader hover");
                }
            }
        }

        private static string WithLine(Trader trader, string text)
        {
            TraderInfo info = TraderInfo.Of(trader);
            int hearts = Friendship.Hearts(Player.m_localPlayer, info);
            if (hearts != info.HoverHearts || text != info.HoverBase || info.HoverVersion != Lang.Version)
            {
                info.HoverHearts = hearts;
                info.HoverBase = text;
                info.HoverVersion = Lang.Version;
                info.HoverText = text + "\n" + Friendship.Line(hearts);
            }
            return info.HoverText;
        }
    }
}
