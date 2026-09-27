using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars as text, "★" (U+2605). The game's text fonts (Averia Sans Libre, Averia Serif Libre) have no star, but
    /// each falls back through the game's own Noto fonts (Fallback-NotoSansNormal, Fallback-NotoSerifNormal) to Noto
    /// Sans JP and Noto Serif JP, which have it, so it renders wherever the game draws TextMeshPro text. Gold is the
    /// colour the game draws a creature's stars in (1, 0.82, 0.34).
    /// </summary>
    public static class StarText
    {
        public const string Gold = "#FFD157";

        private static readonly string[] glyphs = { "", "★", "★★", "★★★" };

        private static readonly string[] colored =
        {
            "", Paint(glyphs[1]), Paint(glyphs[2]), Paint(glyphs[3]),
        };

        /// <summary>The stars as plain glyphs, "" for none.</summary>
        public static string Glyphs(int stars) => glyphs[Mathf.Clamp(stars, 0, Stars.Max)];

        /// <summary>The stars as gold glyphs (a rich text colour tag), "" for none.</summary>
        public static string Colored(int stars) => colored[Mathf.Clamp(stars, 0, Stars.Max)];

        private static string Paint(string text) => "<color=" + Gold + ">" + text + "</color>";
    }
}
