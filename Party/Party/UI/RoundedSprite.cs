using UnityEngine;

namespace Party.UI
{
    /// <summary>A small rounded-rect sprite, 9-sliced so it stays crisp at any size. Built once at runtime (no art assets in this repo).</summary>
    public static class RoundedSprite
    {
        private const int Size = 64;
        private const int Radius = 20;
        private static Sprite sprite;

        public static Sprite Get()
        {
            if (sprite == null)
                sprite = Build();
            return sprite;
        }

        private static Sprite Build()
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(Coverage(x, y) * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            Vector4 border = new Vector4(Radius, Radius, Radius, Radius);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Distance-based soft edge (~1px falloff) instead of a hard cutoff, so the curve doesn't look stair-stepped.</summary>
        private static float Coverage(int x, int y)
        {
            float cx = Mathf.Clamp(x + 0.5f, Radius, Size - Radius);
            float cy = Mathf.Clamp(y + 0.5f, Radius, Size - Radius);
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(Radius - dist + 0.5f);
        }
    }
}
