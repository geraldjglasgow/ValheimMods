using UnityEngine;

namespace Party.UI
{
    /// <summary>A small 9-sliceable rounded-rect texture, built once at runtime (no art assets in this repo).</summary>
    public static class RoundedTexture
    {
        private const int Size = 32;
        private const int Radius = 8;
        private static GUIStyle style;

        /// <summary>A style whose background is the rounded texture, 9-sliced so any rect size stays crisp.</summary>
        public static GUIStyle Style()
        {
            if (style == null)
                style = new GUIStyle { normal = { background = Build() }, border = new RectOffset(Radius, Radius, Radius, Radius) };
            return style;
        }

        private static Texture2D Build()
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = Inside(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static bool Inside(int x, int y)
        {
            float cx = Mathf.Clamp(x, Radius, Size - 1 - Radius);
            float cy = Mathf.Clamp(y, Radius, Size - 1 - Radius);
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= Radius * Radius;
        }
    }
}
