using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars as text, "★" (U+2605). The game's text fonts (Averia Sans Libre, Averia Serif Libre) have no star, but
    /// each falls back through the game's own Noto fonts (Fallback-NotoSansNormal, Fallback-NotoSerifNormal) to Noto
    /// Sans JP and Noto Serif JP, which have it, so it renders wherever the game draws TextMeshPro text. Gold is the
    /// colour the game draws a creature's stars in (1, 0.82, 0.34). Item stars are coloured by their count: one bronze,
    /// two silver, three gold (<see cref="Tier"/>).
    /// </summary>
    public static class StarText
    {
        public const string Gold = "#FFD157";
        public const string Silver = "#D2DAE2";
        public const string Bronze = "#D08A4E";

        private static readonly string[] glyphs = { "", "★", "★★", "★★★" };

        private static readonly string[] colored =
        {
            "", Paint(glyphs[1], Gold), Paint(glyphs[2], Gold), Paint(glyphs[3], Gold),
        };

        private static readonly string[] tiers =
        {
            "", Paint(glyphs[1], Bronze), Paint(glyphs[2], Silver), Paint(glyphs[3], Gold),
        };

        /// <summary>The stars as plain glyphs, "" for none.</summary>
        public static string Glyphs(int stars) => glyphs[Mathf.Clamp(stars, 0, Stars.Max)];

        /// <summary>The stars as gold glyphs (a rich text colour tag), "" for none.</summary>
        public static string Colored(int stars) => colored[Mathf.Clamp(stars, 0, Stars.Max)];

        /// <summary>An item's stars in its tier's colour: bronze, silver or gold; "" for none.</summary>
        public static string Tier(int stars) => tiers[Mathf.Clamp(stars, 0, Stars.Max)];

        /// <summary>An item's tier colour for drawn stars: bronze, silver or gold (gold for none).</summary>
        public static Color TierColor(int stars)
        {
            string hex = stars == 1 ? Bronze : stars == 2 ? Silver : Gold;
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private static string Paint(string text, string color) => "<color=" + color + ">" + text + "</color>";
    }
}
