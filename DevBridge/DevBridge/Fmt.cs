using System;
using System.Globalization;
using System.Text.RegularExpressions;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge
{
    /// <summary>Small conversions between Unity values and the plain text and arrays the bridge sends and receives.</summary>
    internal static class Fmt
    {
        private static readonly Regex GitBashDrive = new Regex("^/([a-zA-Z])/");

        internal static float R(float value) => (float)Math.Round(value, 2);

        internal static float[] V3(Vector3 value) => new[] { R(value.x), R(value.y), R(value.z) };

        /// <summary>One line, at most max characters: newlines become \n.</summary>
        internal static string Clip(string text, int max)
        {
            if (text == null) return "";
            text = text.Replace("\r", "").Replace("\n", "\\n");
            return text.Length <= max ? text : text.Substring(0, max) + "...";
        }

        internal static float[] Numbers(string text, int count, string what)
        {
            string[] parts = (text ?? "").Split(',');
            if (parts.Length != count) throw new BridgeException($"{what} needs {count} comma-separated numbers");
            var numbers = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    throw new BridgeException($"{what}: '{parts[i]}' is not a number");
            }
            return numbers;
        }

        internal static Vector3 ParseV3(string text, string what)
        {
            float[] n = Numbers(text, 3, what);
            return new Vector3(n[0], n[1], n[2]);
        }

        /// <summary>Accepts Windows paths and Git Bash paths (/c/Users/... becomes C:/Users/...).</summary>
        internal static string WindowsPath(string path) =>
            GitBashDrive.Replace(path, match => match.Groups[1].Value.ToUpperInvariant() + ":/");

        /// <summary>A screen point from x= and y= given in pixels from the top-left, as screenshots show it.</summary>
        internal static Vector2 ScreenPoint(float x, float yFromTop) => new Vector2(x, Screen.height - yFromTop);
    }
}
