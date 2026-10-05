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

        // A pointer arrow, tip at the top left, in pixels measured down from the top: white, tinted per player by the
        // image colour, with a dark outline that tinting leaves dark.
        private const int PointerSize = 24;
        private static readonly Vector2[] Arrow =
        {
            new Vector2(1.5f, 1.5f), new Vector2(1.5f, 19f), new Vector2(6f, 14.5f), new Vector2(9.5f, 22f),
            new Vector2(12.5f, 20.5f), new Vector2(9f, 13.5f), new Vector2(15f, 13.5f)
        };
        private static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(1.2f, 0f), new Vector2(-1.2f, 0f), new Vector2(0f, 1.2f), new Vector2(0f, -1.2f),
            new Vector2(0.9f, 0.9f), new Vector2(-0.9f, 0.9f), new Vector2(0.9f, -0.9f), new Vector2(-0.9f, -0.9f)
        };
        private static Sprite pointer;

        /// <summary>The crew's pointer arrow, pivot at its tip.</summary>
        internal static Sprite Pointer => pointer != null ? pointer : (pointer = BuildPointer());

        private static Sprite BuildPointer()
        {
            Texture2D texture = new Texture2D(PointerSize, PointerSize, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < PointerSize; y++)
            {
                for (int x = 0; x < PointerSize; x++)
                    texture.SetPixel(x, y, PointerPixel(new Vector2(x + 0.5f, PointerSize - y - 0.5f)));
            }
            texture.Apply();
            Vector2 tip = new Vector2(Arrow[0].x / PointerSize, 1f - Arrow[0].y / PointerSize);
            return Sprite.Create(texture, new Rect(0, 0, PointerSize, PointerSize), tip, PointerSize);
        }

        private static Color PointerPixel(Vector2 point)
        {
            if (InArrow(point))
                return Color.white;
            foreach (Vector2 offset in OutlineOffsets)
            {
                if (InArrow(point + offset))
                    return new Color(0.05f, 0.05f, 0.05f, 0.9f);
            }
            return Color.clear;
        }

        /// <summary>Even-odd ray cast against the arrow's outline.</summary>
        private static bool InArrow(Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = Arrow.Length - 1; i < Arrow.Length; j = i++)
            {
                Vector2 a = Arrow[i];
                Vector2 b = Arrow[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
