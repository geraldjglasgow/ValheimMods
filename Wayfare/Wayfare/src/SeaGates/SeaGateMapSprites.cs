using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>The sea gate map icons, drawn once at runtime so nothing ships as a binary asset: a dark sea-blue disc
    /// with two pale waves across it inside a ring, teal for a gate, gold for the gate the picker was opened at. Unlike
    /// the portals' gold portal icon (<c>Targeting.IconFactory</c>), so the two never read as the same thing.</summary>
    internal static class SeaGateMapSprites
    {
        private const int Size = 32;
        private const float Ring = 3.5f;
        private const float WaveHalfWidth = 1.1f;
        private static readonly Color GateRing = new Color(0.25f, 0.85f, 0.8f, 1f);
        private static readonly Color SourceRing = new Color(1f, 0.82f, 0.3f, 1f);
        private static readonly Color Fill = new Color(0.05f, 0.2f, 0.3f, 0.85f);
        private static readonly Color Wave = new Color(0.85f, 0.97f, 1f, 1f);

        private static Sprite gate;
        private static Sprite source;

        internal static Sprite Gate => gate != null ? gate : (gate = Build(GateRing));

        internal static Sprite Source => source != null ? source : (source = Build(SourceRing));

        private static Sprite Build(Color ring)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = Pixel(x, y, ring);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        /// <summary>One pixel: outside clear, then the ring, then the waves over the fill, each edge softened by a pixel.</summary>
        private static Color Pixel(int x, int y, Color ring)
        {
            float center = (Size - 1) / 2f;
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            float outer = Size / 2f - 1f;
            if (distance > outer)
                return WithAlpha(ring, Mathf.Clamp01(outer + 1f - distance));
            Color inside = Color.Lerp(Fill, Wave, WaveCover(x - center, y - center));
            return Color.Lerp(inside, ring, Mathf.Clamp01(distance - (outer - Ring) + 0.5f));
        }

        /// <summary>How much of a pixel two sine waves cover, one above and one below the middle, a little over one
        /// period across the disc.</summary>
        private static float WaveCover(float dx, float dy)
        {
            float phase = dx / Size * Mathf.PI * 2.5f;
            float upper = 3.5f + 2f * Mathf.Sin(phase);
            float lower = -3.5f + 2f * Mathf.Sin(phase + 0.6f);
            float nearest = Mathf.Min(Mathf.Abs(dy - upper), Mathf.Abs(dy - lower));
            return Mathf.Clamp01(WaveHalfWidth + 0.5f - nearest);
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
    }
}
