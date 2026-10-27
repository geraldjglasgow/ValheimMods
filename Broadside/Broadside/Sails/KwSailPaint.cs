using UnityEngine;

namespace Broadside
{
    /// <summary>An emblem's picture (<see cref="KwSailEmblems"/>): its pixels (bottom row first) and how wide it is drawn for its height.</summary>
    public sealed class KwEmblemArt
    {
        public KwEmblemArt(Color32[] pixels, int width, int height, float aspect)
        {
            (Pixels, Width, Height, Aspect) = (pixels, width, height, aspect);
        }

        public Color32[] Pixels { get; }
        public int Width { get; }
        public int Height { get; }
        public float Aspect { get; }

        /// <summary>The pixel under a place in the picture (0 to 1 each way).</summary>
        public Color32 At(float x, float y)
        {
            int i = Mathf.Clamp((int)(x * Width), 0, Width - 1), j = Mathf.Clamp((int)(y * Height), 0, Height - 1);
            return Pixels[j * Width + i];
        }
    }

    /// <summary>
    /// A pattern (<see cref="KwSailPattern"/>) painted over a sail cloth's own texture, texel by texel, in the texture's
    /// own places (<see cref="KwSailFrame"/>), so it lies on the cloth however it is set, reefed or furled. The colour is a
    /// dye: each texel takes the colour at the cloth's own lightness there (its stitched panels, its hide's grain and its
    /// shading stay), with a little of the cloth's own colour left in; the cloth's cut-out edges are kept. An emblem sits on
    /// a round field of the colour, drawn over it in its own colours on the same cloth. Plain arithmetic on pixels, the same
    /// on every machine for the same pattern, cloth and proportions.
    /// </summary>
    public static class KwSailPaint
    {
        private const float Kept = 0.15f, BorderShare = 0.075f, ArtShare = 0.92f;

        /// <summary>The cloth's pixels (size by size, bottom row first) with the pattern painted on; art null leaves an emblem's field bare.</summary>
        public static Color32[] Paint(Color32[] cloth, int size, KwSailPattern pattern, KwSailFrame frame, KwEmblemArt? art)
        {
            Color32[] made = new Color32[cloth.Length];
            Job job = new Job(pattern, frame, art, Mean(cloth));
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size, y = (j + 0.5f) / size;
                    Color32 own = cloth[j * size + i];
                    float shade = Mathf.Clamp(Lightness(own) / job.Mean, 0.6f, 1.35f);
                    Vector2 on = frame.On(x, y);
                    Color32 texel = Dyed(job, x, y, on) ? Dye(own, job.Dye, shade) : own;
                    made[j * size + i] = art != null && pattern.HasEmblem ? Emblem(texel, own, shade, art, on - job.Centre, job.Radius) : texel;
                }
            }
            return made;
        }

        // What every texel of one painting needs, worked out once.
        private sealed class Job
        {
            public Job(KwSailPattern pattern, KwSailFrame frame, KwEmblemArt? art, float mean)
            {
                (Pattern, Split, Mean, Dye) = (pattern, frame.Split, mean, KwSailDye.Colour(pattern.Colour));
                (Centre, Radius) = frame.Middle;
                Border = BorderShare * frame.Least;
                Edge = System.Array.ConvertAll(frame.Corners, c => frame.On(c.x, c.y));
                Box = frame.Box;
            }

            public KwSailPattern Pattern { get; }

            /// <summary>The cloth's reach in the texture (a jib's triangle's), that stripes spread over.</summary>
            public Rect Box { get; }
            public Vector2 Split { get; }
            public float Mean { get; }
            public Color32 Dye { get; }
            public Vector2 Centre { get; }
            public float Radius { get; }
            public float Border { get; }
            public Vector2[] Edge { get; }
        }

        // Whether the pattern's colour covers a texel: x, y its texture place, on its place on the cloth.
        private static bool Dyed(Job job, float x, float y, Vector2 on)
        {
            switch (job.Pattern.Kind)
            {
                case KwSailPattern.Upright:
                    return Band((x - job.Box.xMin) / job.Box.width, job.Pattern.Count);
                case KwSailPattern.Across:
                    return Band((y - job.Box.yMin) / job.Box.height, job.Pattern.Count);
                case KwSailPattern.Quarters:
                    return (x < job.Split.x) == (y >= job.Split.y);
                case KwSailPattern.Border:
                    return KwSailFrame.Inside(job.Edge, on) < job.Border;
                case KwSailPattern.Emblem:
                    return (on - job.Centre).sqrMagnitude < job.Radius * job.Radius;
                default:
                    return false;
            }
        }

        // Stripes: 2n + 1 even bands, the cloth's first and last, the colour's every other one between.
        private static bool Band(float t, int stripes) => Mathf.Min((int)(t * (2 * stripes + 1)), 2 * stripes) % 2 == 1;

        // The colour at the cloth's lightness there, a little of the cloth's own kept; its cut-out kept.
        private static Color32 Dye(Color32 own, Color32 dye, float shade) => new Color32(
            Mix(dye.r * shade, own.r, Kept), Mix(dye.g * shade, own.g, Kept), Mix(dye.b * shade, own.b, Kept), own.a);

        // The emblem's picture over a texel, in the box that fits inside its field; at the cloth's lightness as the dye is.
        private static Color32 Emblem(Color32 texel, Color32 own, float shade, KwEmblemArt art, Vector2 off, float radius)
        {
            float high = ArtShare * radius / Mathf.Sqrt(1f + art.Aspect * art.Aspect), wide = high * art.Aspect;
            float ex = (off.x / wide + 1f) * 0.5f, ey = (off.y / high + 1f) * 0.5f;
            if (ex < 0f || ex >= 1f || ey < 0f || ey >= 1f)
                return texel;
            Color32 pixel = art.At(ex, ey);
            float cover = pixel.a / 255f;
            if (cover <= 0.01f)
                return texel;
            Color32 painted = new Color32(Mix(pixel.r * shade, own.r, Kept * 0.6f), Mix(pixel.g * shade, own.g, Kept * 0.6f), Mix(pixel.b * shade, own.b, Kept * 0.6f), own.a);
            return Color32.Lerp(texel, painted, cover);
        }

        private static byte Mix(float colour, byte own, float kept) => (byte)Mathf.Clamp(Mathf.Round(colour * (1f - kept) + own * kept), 0f, 255f);

        private static float Lightness(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // The cloth's mean lightness where it is not cut out.
        private static float Mean(Color32[] cloth)
        {
            double sum = 0;
            int count = 0;
            foreach (Color32 c in cloth)
            {
                if (c.a < 128)
                    continue;
                sum += Lightness(c);
                count++;
            }
            return count > 0 && sum > 0 ? (float)(sum / count) : 128f;
        }
    }
}
