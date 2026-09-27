using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The words of the trash filter: its four settings, the hover line and the message after a change. The star is
    /// U+2605. The game's Averia and Norse fonts lack it, but their TextMeshPro fallback lists (Fallback-NotoSansNormal,
    /// Fallback-NotoSerifNormal) hold the dynamic Noto CJK font assets, whose fonts have it, so it renders through the
    /// fallback. The key hint is the game's own markup, the same a tamed animal's rename hint uses.
    /// </summary>
    public static class FilterText
    {
        public const string Star = "★";

        private const string AltHint = "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>]";
        private const string GamepadAltHint = "[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>]";

        /// <summary>What a kitchen with this minimum keeps: "all dishes", "1★ and up", "2★ and up" or "3★ only".</summary>
        public static string Keeps(int minStars)
        {
            minStars = Mathf.Clamp(minStars, 0, Stars.Max);
            if (minStars == 0)
                return "all dishes";
            return minStars == Stars.Max ? $"{minStars}{Star} only" : $"{minStars}{Star} and up";
        }

        /// <summary>The local player's chance, in whole percent, to make a dish with at least these stars.</summary>
        public static int ChancePercent(int minStars)
        {
            float chance = StarOdds.ChanceAtLeast(CookLevel.Effective(CookLevel.Local(), 0f), minStars);
            return Mathf.RoundToInt(chance * 100f);
        }

        /// <summary>The setting with the local player's chance of a kept dish; "keep all" shows no chance.</summary>
        public static string Setting(int minStars)
        {
            string keeps = Keeps(minStars);
            return minStars <= 0 ? keeps : $"{keeps} (your chance {ChancePercent(minStars)}%)";
        }

        /// <summary>The hover line: the key hint when the local player may change the setting, then the setting.</summary>
        public static string HoverLine(int minStars, bool canChange)
        {
            string hint = canChange ? Localization.instance.Localize(KeyHint()) + " " : "";
            return "\n" + hint + "Keeps: " + Setting(minStars);
        }

        /// <summary>The message after a change.</summary>
        public static string Changed(int minStars) => "Trash filter: keeps " + Setting(minStars);

        /// <summary>The game's alternative-use hint: the alt keys on a gamepad with the newer layout, AltPlace otherwise.</summary>
        private static string KeyHint() => ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? GamepadAltHint : AltHint;
    }
}
