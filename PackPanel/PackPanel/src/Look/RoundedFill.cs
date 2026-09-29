using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// A white rounded rectangle as a 9-slice sprite, painted once in code (no image file): tinted by an Image's colour
    /// it gives a fill round corners (the Gear tab's stat sheet, the user's request 2026-09-28). The corners are
    /// <see cref="Radius"/> UI units whatever the size; their edge is anti-aliased over one texture pixel, the straight
    /// edges fall on pixel boundaries and stay crisp.
    /// </summary>
    public static class RoundedFill
    {
        public const float Radius = 6f;

        /// <summary>The corner's radius in texture pixels: fine enough to stay smooth on a 4K screen.</summary>
        private const int Corner = 16;

        private const int Size = Corner * 2 + 2;
        private static Sprite sprite;

        public static Sprite Sprite => sprite != null ? sprite : sprite = Paint();

        private static Sprite Paint()
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "PackPanel_rounded",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(255f * Coverage(x + 0.5f, y + 0.5f)));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite made = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Corner * 100f / Radius,
                0u, SpriteMeshType.FullRect, new Vector4(Corner, Corner, Corner, Corner));
            made.name = "PackPanel_rounded";
            made.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return made;
        }

        /// <summary>How much of a pixel centred at (x, y) lies inside the rounded outline, 0 to 1.</summary>
        private static float Coverage(float x, float y)
        {
            float dx = Mathf.Max(Mathf.Max(Corner - x, x - (Size - Corner)), 0f);
            float dy = Mathf.Max(Mathf.Max(Corner - y, y - (Size - Corner)), 0f);
            return Mathf.Clamp01(Corner - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }
    }
}
