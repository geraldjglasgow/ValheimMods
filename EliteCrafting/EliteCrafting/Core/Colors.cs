using UnityEngine;

namespace EliteCrafting.Core
{
    /// <summary><c>#RRGGBB</c> ↔ <see cref="Color32"/>, our own parse (no culture, no named colors).</summary>
    public static class Colors
    {
        public static bool TryParse(string? text, out Color32 color)
        {
            color = new Color32(255, 255, 255, 255);
            if (text == null || text.Length != 7 || text[0] != '#')
            {
                return false;
            }
            if (!Hex(text, 1, out byte r) || !Hex(text, 3, out byte g) || !Hex(text, 5, out byte b))
            {
                return false;
            }
            color = new Color32(r, g, b, 255);
            return true;
        }

        private static bool Hex(string text, int at, out byte value)
        {
            return byte.TryParse(text.Substring(at, 2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }
    }
}
