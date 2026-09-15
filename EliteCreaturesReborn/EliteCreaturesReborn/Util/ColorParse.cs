using UnityEngine;

namespace EliteCreaturesReborn.Util
{
    /// <summary>Parses the "#RRGGBB" hex strings the palette config holds into Unity colours.</summary>
    internal static class ColorParse
    {
        public static Color Hex(string text, Color fallback)
        {
            if (!string.IsNullOrWhiteSpace(text) && ColorUtility.TryParseHtmlString(text.Trim(), out Color color))
            {
                return color;
            }
            return fallback;
        }
    }
}
