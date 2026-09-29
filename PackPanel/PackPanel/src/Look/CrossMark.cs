using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// A white cross (an X with round ends) as a sprite, painted once in code (no image file): tinted grey it marks a
    /// cell a worn backpack does not open (<see cref="Panels.BlockedCell"/>). Its strokes are anti-aliased over one pixel.
    /// </summary>
    public static class CrossMark
    {
        private const int Size = 64;
        private const float Margin = 14f;
        private const float HalfWidth = 2.5f;
        private static Sprite sprite;

        public static Sprite Sprite => sprite != null ? sprite : sprite = Paint();

        private static Sprite Paint()
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "PackPanel_cross",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255f * Coverage(new Vector2(x + 0.5f, y + 0.5f))));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite made = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            made.name = "PackPanel_cross";
            made.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return made;
        }

        /// <summary>How much of the pixel centred at <paramref name="p"/> the two strokes cover, 0 to 1.</summary>
        private static float Coverage(Vector2 p)
        {
            float low = Margin, high = Size - Margin;
            float d = Mathf.Min(Distance(p, new Vector2(low, low), new Vector2(high, high)),
                Distance(p, new Vector2(low, high), new Vector2(high, low)));
            return Mathf.Clamp01(HalfWidth - d + 0.5f);
        }

        /// <summary>From a point to the segment a-b.</summary>
        private static float Distance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
