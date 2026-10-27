using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth as its texture lies on it, for painting a pattern there (<see cref="KwSailPaint"/>): the part of the
    /// texture the cloth shows fully set (<see cref="KwSailCanvas"/>: a square sail all of it, a triangle sail the corner
    /// under its diagonal, a gaff sail all but the slant over its head, from three quarters up at the mast to the top at
    /// its after edge; a jib its own triangle, cut from the texture at one scale both ways, <see cref="KwJibUv"/>), and how
    /// a place in the texture lies on the cloth in metres: x along its fastened edge or foot, y up it. Only the proportions
    /// count (a border and an emblem are sized to the sail), kept to steps so that sails much alike share their painting
    /// (<see cref="Key"/>).
    /// </summary>
    public readonly struct KwSailFrame
    {
        private const float AspectStep = 0.08f, AngleStep = 3f, JibSide = 0.03f, JibHead = 0.03f;

        private KwSailFrame(int shape, float aspect, float angle, float scale)
        {
            (Shape, Aspect, Angle, Scale) = (shape, aspect, angle, scale);
        }

        /// <summary>0 square, 1 triangle, 2 gaff (<see cref="KwSailSpec.Shapes"/>).</summary>
        public int Shape { get; }

        /// <summary>The cloth's width over its height.</summary>
        public float Aspect { get; }

        /// <summary>The angle between the foot and the luff, degrees (90 on a mast).</summary>
        public float Angle { get; }

        /// <summary>A jib's texture across its luff (its luff over the metres the texture spans); 0 for as large as fits.</summary>
        public float Scale { get; }

        /// <summary>
        /// A sail of that shape, width and height (a jib's luff at that angle to its foot, its texture spanning that many
        /// metres of cloth when it fits), on the steps: a jib's finer, as its texture is laid on by its frame
        /// (<see cref="KwJibUv"/>), so it is within 1 to 4 % of square on the cloth.
        /// </summary>
        public static KwSailFrame Of(int shape, float width, float height, float angle = 90f, float metres = 0f)
        {
            bool jib = shape == 1 && angle < 89.5f;
            float step = jib ? AspectStep / 4f : AspectStep, turn = jib ? 1f : AngleStep;
            float aspect = Stepped(Mathf.Max(width, 0.1f) / Mathf.Max(height, 0.1f), step);
            float lean = jib ? Mathf.Clamp(Mathf.Round(angle / turn) * turn, 15f, 88f) : 90f;
            float scale = jib && metres > 0.1f ? Stepped(Mathf.Max(height, 0.1f) / metres, step) : 0f;
            return new KwSailFrame(Mathf.Clamp(shape, 0, 2), aspect, lean, scale);
        }

        // A ratio on steps of a share of itself.
        private static float Stepped(float ratio, float step) => Mathf.Exp(Mathf.Round(Mathf.Log(ratio) / step) * step);

        /// <summary>What the painting depends on, for sharing it.</summary>
        public string Key => Shape + ":" + Aspect.ToString("0.###") + ":" + Angle.ToString("0") + (Jib ? ":" + Scale.ToString("0.###") : "");

        /// <summary>A jib's frame: a triangle whose luff is not square to its foot (a mast's triangle sail's is).</summary>
        public bool Jib => Shape == 1 && Angle < 89.5f;

        /// <summary>A texture place (0 to 1 each way) on the cloth, in heights of the sail (a jib's: along its foot and square to it).</summary>
        public Vector2 On(float x, float y)
        {
            if (!Jib)
                return new Vector2(x * Aspect, y);
            (float scale, float left) = JibFit;
            return new Vector2((x - left) / scale, y / scale);
        }

        /// <summary>The cloth's corners in the texture, round its edge (a jib's tack, clew and head).</summary>
        public Vector2[] Corners => Shape == 0 ? new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) }
            : Jib ? JibCorners
            : Shape == 1 ? new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) }
            : new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, KwSailCanvas.Gaff(0f)) };

        // A jib in the texture: its triangle in heights of its luff (tack, clew at its foot's length, head at its angle) at
        // one scale both ways, its own (as much cloth a metre as its foremast's sail) or, where that would not fit, as large
        // as fits a little in from the texture's sides and head; its foot on the texture's foot, in the middle across. The
        // scale, and where its tack is across.
        private (float Scale, float Left) JibFit
        {
            get
            {
                float a = Angle * Mathf.Deg2Rad, wide = Mathf.Max(Aspect, Mathf.Cos(a)), high = Mathf.Sin(a);
                float fits = Mathf.Min((1f - 2f * JibSide) / wide, (1f - JibHead) / high);
                float scale = Scale > 0f ? Mathf.Min(Scale, fits) : fits;
                return (scale, (1f - scale * wide) / 2f);
            }
        }

        private Vector2[] JibCorners
        {
            get
            {
                (float scale, float left) = JibFit;
                float a = Angle * Mathf.Deg2Rad;
                return new[] { new Vector2(left, 0f), new Vector2(left + scale * Aspect, 0f), new Vector2(left + scale * Mathf.Cos(a), scale * Mathf.Sin(a)) };
            }
        }

        /// <summary>How far the cloth reaches in the texture each way (all of it, but for a jib): stripes are spread over it.</summary>
        public Rect Box
        {
            get
            {
                Vector2[] corners = Corners;
                Vector2 low = corners[0], high = corners[0];
                foreach (Vector2 c in corners)
                    (low, high) = (Vector2.Min(low, c), Vector2.Max(high, c));
                return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            }
        }

        /// <summary>The cloth's shortest way across, in heights of the sail: a border's width and an emblem's size go by it.</summary>
        public float Least => Shape == 1 ? 2f * Middle.Radius : Mathf.Min(Aspect, Shape == 2 ? KwSailCanvas.Gaff(0f) : 1f);

        /// <summary>Where a quarters pattern splits, in the texture: the cloth's middle (a triangle's centroid).</summary>
        public Vector2 Split => Shape == 0 ? new Vector2(0.5f, 0.5f) : Shape == 1 ? Centroid(Corners) : new Vector2(0.5f, 0.45f);

        private static Vector2 Centroid(Vector2[] c) => (c[0] + c[1] + c[2]) / 3f;

        /// <summary>
        /// The emblem's round field on the cloth (heights of the sail): in the middle of a square or gaff sail, a third of the
        /// way across its least width; on a triangle its incircle, a little in from the edges.
        /// </summary>
        public (Vector2 Centre, float Radius) Middle
        {
            get
            {
                if (Shape != 1)
                {
                    Vector2 centre = Shape == 0 ? On(0.5f, 0.5f) : On(0.5f, 0.44f);
                    return (centre, 0.3f * Mathf.Min(Aspect, Shape == 2 ? KwSailCanvas.Gaff(0f) : 1f));
                }
                Vector2[] k = Corners;
                Vector2 a = On(k[0].x, k[0].y), b = On(k[1].x, k[1].y), c = On(k[2].x, k[2].y);
                float ab = (b - a).magnitude, bc = (c - b).magnitude, ca = (a - c).magnitude, sum = ab + bc + ca;
                float area = 0.5f * Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
                return ((a * bc + b * ca + c * ab) / sum, 0.9f * 2f * area / sum);
            }
        }

        /// <summary>
        /// How far a point on the cloth is inside its edge (the corners on the cloth, <see cref="On"/>, in order round it): the
        /// least of its distances to the edges, under 0 outside.
        /// </summary>
        public static float Inside(Vector2[] edge, Vector2 p)
        {
            float least = float.MaxValue;
            for (int i = 0; i < edge.Length; i++)
            {
                Vector2 a = edge[i], along = (edge[(i + 1) % edge.Length] - a).normalized;
                least = Mathf.Min(least, Vector2.Dot(p - a, new Vector2(-along.y, along.x)));
            }
            return least;
        }
    }
}
