using UnityEngine;

namespace Party
{
    public static class ColorHelper
    {
        public static Color Parse(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
