using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A tier's blade on one post: where its foot and tip are (heights in the piece's frame, the rudder point at 0) and how
    /// the tier's outline (<see cref="KwRudderTier.Edge"/>) is stretched to them. Its foot stands just over the keel bottom:
    /// at the rudder point on a flat stern, or hanging below a high stern's rudder point by its drop
    /// (<see cref="KwRudderFit.Drop"/>, to 15 cm over the keel bottom). Its broad part reaches up to the ship's design
    /// waterline (<see cref="KwRudderFit.Water"/>): the outline is stretched so that all but <see cref="Dry"/> of its area
    /// is under that line (<see cref="KwRudderTier.Waterline"/>), only the top of its shoulder out of the water, and the
    /// stock goes on up alone to the head over the post's top. A deep ship so has a deep rudder: its chord, its details
    /// and its thickness grow with its height, a little less than in step (its height over the tier's own outline's, to the
    /// power 0.8). It stays under the post's top and, under a steering gear's quadrant, no higher than the quadrant allows
    /// (a ceiling), the outline stretched to that height the same way. While the waterline is not known it is taken
    /// halfway up from the blade's foot to the post's top.
    /// </summary>
    public readonly struct KwRudderSpan
    {
        /// <summary>The share of the blade's area over the design waterline.</summary>
        public const float Dry = 0.04f;

        // The chord's growth with the height; the least height; where a waterline not known is taken; how far under the
        // post's top the blade stops (by the post's scale); how finely the outline's waterline is looked for.
        private const float Growth = 0.8f, Least = 0.3f, Guess = 0.5f, UnderTop = 0.1f;
        private const int Halvings = 24;
        private readonly Vector2[] edge;

        public KwRudderSpan(KwRudderTier tier, KwRudderFit fit, float ceiling = float.MaxValue)
        {
            edge = tier.Edge;
            float first = edge[0].x, own = edge[edge.Length - 1].x - first;
            Foot = fit.Drop > 0f ? -fit.Drop : first * fit.Scale;
            float water = float.IsNaN(fit.Water) ? Foot + Guess * (fit.Height - Foot) : fit.Water;
            float tall = (water - Foot) * own / Mathf.Max(tier.Waterline - first, 0.01f);
            Tip = Mathf.Max(Foot + Least, Mathf.Min(Foot + tall, Mathf.Min(fit.Height - UnderTop * fit.Scale, ceiling)));
            Chord = Mathf.Pow((Tip - Foot) / own, Growth);
            Thick = Mathf.Clamp(Mathf.Sqrt(Chord), 0.35f, 1.5f);
        }

        /// <summary>The blade's lowest and highest points.</summary>
        public float Foot { get; }
        public float Tip { get; }

        /// <summary>The scale of the outline's distances aft and of the blade's details: 1 for a blade as tall as its tier's outline.</summary>
        public float Chord { get; }

        /// <summary>The blade's thickness factor, as <see cref="KwRudderFit.Thick"/> is the post's.</summary>
        public float Thick { get; }

        /// <summary>The height on the blade of a height on the tier's outline.</summary>
        public float Z(float outline) => Mathf.Lerp(Foot, Tip, Mathf.InverseLerp(edge[0].x, edge[edge.Length - 1].x, outline));

        /// <summary>The blade's distance aft of the stock at height z.</summary>
        public float AftAt(float z)
        {
            float h = Mathf.Lerp(edge[0].x, edge[edge.Length - 1].x, Mathf.InverseLerp(Foot, Tip, z));
            for (int i = 0; i + 1 < edge.Length; i++)
            {
                if (h <= edge[i + 1].x)
                    return Mathf.Lerp(edge[i].y, edge[i + 1].y, Mathf.InverseLerp(edge[i].x, edge[i + 1].x, h)) * Chord;
            }
            return edge[edge.Length - 1].y * Chord;
        }

        /// <summary>The height on an outline (height, distance aft) below which a share of its area lies.</summary>
        public static float Wet(Vector2[] outline, float share)
        {
            float low = outline[0].x, high = outline[outline.Length - 1].x, wanted = share * Area(outline, high);
            for (int i = 0; i < Halvings; i++)
            {
                float mid = (low + high) / 2f;
                if (Area(outline, mid) < wanted)
                    low = mid;
                else
                    high = mid;
            }
            return (low + high) / 2f;
        }

        // An outline's area from its foot up to a height, a trapezium a corner.
        private static float Area(Vector2[] outline, float height)
        {
            float area = 0f;
            for (int i = 0; i + 1 < outline.Length && outline[i].x < height; i++)
            {
                Vector2 a = outline[i], b = outline[i + 1];
                float top = Mathf.Min(height, b.x), aft = Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, top));
                area += (a.y + aft) / 2f * (top - a.x);
            }
            return area;
        }
    }
}
