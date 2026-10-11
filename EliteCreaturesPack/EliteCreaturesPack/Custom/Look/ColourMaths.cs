using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The colour changes the look step makes, each on one colour: a tint (the game's own way: a material's <c>_Color</c>
    /// multiplies its texture, so a tint replaces that colour), and for an overlay effect a colourize (the overlay colour's
    /// hue and saturation, the colour's own brightness times the overlay's) or a grey (the colour's brightness alone, for
    /// the parts that only multiply the colour that carries the hue). Brightness above 1 (the game's HDR fire) is kept.
    /// </summary>
    internal static class ColourMaths
    {
        /// <summary>The tint's red, green and blue in place of the colour's, its alpha kept (cutout fur reads alpha).</summary>
        public static Color Tinted(Color colour, Color tint) => new Color(tint.r, tint.g, tint.b, colour.a);

        /// <summary>
        /// The target's hue and saturation at the colour's brightness times the target's; with <paramref name="alpha"/> the
        /// colour's alpha is multiplied by the target's, so an overlay colour with alpha draws fainter.
        /// </summary>
        public static Color Colourize(Color colour, Color target, bool alpha)
        {
            Color.RGBToHSV(colour, out _, out _, out float value);
            Color.RGBToHSV(target, out float hue, out float saturation, out float targetValue);
            Color made = Color.HSVToRGB(hue, saturation, value * targetValue, true);
            made.a = alpha ? colour.a * target.a : colour.a;
            return made;
        }

        /// <summary>The colour's brightness as a grey, its alpha kept.</summary>
        public static Color Grey(Color colour)
        {
            Color.RGBToHSV(colour, out _, out _, out float value);
            return new Color(value, value, value, colour.a);
        }
    }
}
