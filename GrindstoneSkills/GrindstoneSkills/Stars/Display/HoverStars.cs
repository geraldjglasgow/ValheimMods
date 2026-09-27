using System;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A starred dish lying on the ground shows its stars when looked at. ItemDrop.GetHoverText (the crosshair text)
    /// loads the item from its ZDO, writes any quality above 1 straight after the name as "[3] " (so a 2-star dish
    /// would read "Cooked meat[3]  x5") and localizes the result; that tag becomes the stars. If another mod removed the
    /// tag, the stars go at the end of the first line. GetHoverName (the unlocalized name, used in messages such as
    /// "can't use X on Y") gets the stars appended as plain glyphs.
    /// </summary>
    public static class HoverStars
    {
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
        private static class Text
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop __instance, ref string __result)
            {
                int stars = Stars.Get(__instance.m_itemData);
                if (stars > 0 && __result != null)
                    __result = WithStars(__result, __instance.m_itemData.m_quality, stars);
            }
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverName))]
        private static class Name
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop __instance, ref string __result)
            {
                int stars = Stars.Get(__instance.m_itemData);
                if (stars > 0)
                    __result += " " + StarText.Glyphs(stars);
            }
        }

        private static string WithStars(string text, int quality, int stars)
        {
            string glyphs = " " + StarText.Colored(stars);
            string tag = "[" + quality + "] ";
            int at = text.IndexOf(tag, StringComparison.Ordinal);
            if (at >= 0)
                return text.Substring(0, at) + glyphs + text.Substring(at + tag.Length);
            int line = text.IndexOf('\n');
            return line < 0 ? text + glyphs : text.Insert(line, glyphs);
        }
    }
}
