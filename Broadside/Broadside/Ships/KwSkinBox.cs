using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One of a launched ship's boxes lying on a stretch of its hull's face (<see cref="KwHullSkin"/>,
    /// <see cref="KwShipTransomWall"/>), in its part's kit axes: its middle, the way along it (the box's z), the way up it
    /// (its y, square to along) and the way out of the hull (its x, the thin way), and its size in its own axes (thin, up,
    /// along) as a <see cref="BoxCollider"/> takes it. Pure but for <see cref="Turn"/>.
    /// </summary>
    public readonly struct KwSkinBox
    {
        public KwSkinBox(Vector3 centre, Vector3 along, Vector3 up, Vector3 outward, Vector3 size) =>
            (Centre, Along, Up, Out, Size) = (centre, along, up, outward, size);

        public Vector3 Centre { get; }
        public Vector3 Along { get; }
        public Vector3 Up { get; }
        public Vector3 Out { get; }
        public Vector3 Size { get; }

        /// <summary>Its broad face from its middle: x along, y up.</summary>
        public Rect Face => Rect.MinMaxRect(-Size.z / 2f, -Size.y / 2f, Size.z / 2f, Size.y / 2f);

        /// <summary>How it is turned in its part's Unity axes (the game's own quaternion maths, so in game only).</summary>
        public Quaternion Turn => Quaternion.LookRotation(KwMesh.ToUnity(Along), KwMesh.ToUnity(Up));

        /// <summary>The box over part of its face (x along, y up from its middle), as thick and turned as it is.</summary>
        public KwSkinBox Part(Rect r) => new KwSkinBox(Centre + Along * r.center.x + Up * r.center.y, Along, Up, Out, new Vector3(Size.x, r.height, r.width));
    }

    /// <summary>
    /// The box over a four-cornered stretch of a hull's face (<see cref="KwHullSkin"/>): its first edge from a0 up to a1, its
    /// second from c0 up to c1, as the planking's strakes run between two stations. It is laid one of three ways
    /// (<see cref="Along"/>, <see cref="Upright"/>, <see cref="Top"/>), one way of it the stretch's own and the other square
    /// to that; its broad face covers the corners seen square to it, a little over
    /// (<see cref="Margin"/>, so neighbours close on each other), its outer face on the corners' middle plane, a stand off
    /// it (the laps' half on planking), <see cref="Thick"/> deep into the hull. How far the corners lie off that plane says
    /// how twisted the stretch is. Pure.
    /// </summary>
    public static class KwSkinFit
    {
        /// <summary>How deep a box is, inward from its outer face: the planking's thickness and a little.</summary>
        public const float Thick = 0.1f;

        /// <summary>How far a box's face runs on past its stretch on every side.</summary>
        public const float Margin = 0.01f;

        /// <summary>The least a box's face is along and up (its thin way stays its thickness, <see cref="KwHoleSlab"/>).</summary>
        public const float Least = 0.12f;

        /// <summary>
        /// How a box is laid on its stretch: along the strakes (the edges' way between the stations), upright (the edges'
        /// way up, its ends on the stations), or along its top edge (the sheer, so it stands nothing over it).
        /// </summary>
        public const int Along = 0, Upright = 1, Top = 2;

        /// <summary>The box over a stretch, outward its way out, and its corners' most off its plane; false for a stretch with no length or height.</summary>
        public static bool Of(Vector3 a0, Vector3 a1, Vector3 c0, Vector3 c1, Vector3 outward, float stand, int lay, out KwSkinBox box, out float off)
        {
            (box, off) = (default, 0f);
            Vector3 along = lay == Top ? c1 - a1 : c0 - a0 + c1 - a1;
            if (!Axes(along, a1 - a0 + c1 - c0, outward, lay == Upright, out Vector3 u, out Vector3 v, out Vector3 n))
                return false;
            Vector3 mid = (a0 + a1 + c0 + c1) / 4f;
            Vector2 lo = Vector2.positiveInfinity, hi = Vector2.negativeInfinity;
            Seen(a0 - mid, u, v, n, ref lo, ref hi, ref off);
            Seen(a1 - mid, u, v, n, ref lo, ref hi, ref off);
            Seen(c0 - mid, u, v, n, ref lo, ref hi, ref off);
            Seen(c1 - mid, u, v, n, ref lo, ref hi, ref off);
            Rect face = Grown(Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y));
            Vector3 centre = mid + u * face.center.x + v * face.center.y + n * (stand - Thick / 2f);
            box = new KwSkinBox(centre, u, v, n, new Vector3(Thick, face.height, face.width));
            return true;
        }

        // A corner seen square to the box's face (its reach widened) and off its plane (the most kept).
        private static void Seen(Vector3 p, Vector3 u, Vector3 v, Vector3 n, ref Vector2 lo, ref Vector2 hi, ref float off)
        {
            Vector2 seen = new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v));
            (lo, hi, off) = (Vector2.Min(lo, seen), Vector2.Max(hi, seen), Mathf.Max(off, Mathf.Abs(Vector3.Dot(p, n))));
        }

        /// <summary>
        /// How far a box's face runs past its stretch seen square to it, at its farthest corner (its margin and least size
        /// included): nothing much for a stretch that is near a rectangle.
        /// </summary>
        public static float Over(KwSkinBox box, Vector3 a0, Vector3 a1, Vector3 c0, Vector3 c1)
        {
            KwSkinQuad2 quad = new KwSkinQuad2(On(box, a0), On(box, a1), On(box, c1), On(box, c0));
            Rect face = box.Face;
            float most = Mathf.Max(quad.Outside(face.min), quad.Outside(face.max));
            return Mathf.Max(most, Mathf.Max(quad.Outside(new Vector2(face.xMin, face.yMax)), quad.Outside(new Vector2(face.xMax, face.yMin))));
        }

        private static Vector2 On(KwSkinBox box, Vector3 p) => new Vector2(Vector3.Dot(p - box.Centre, box.Along), Vector3.Dot(p - box.Centre, box.Up));

        /// <summary>The middle of a box's face on the stretch's plane (its outer face less the stand).</summary>
        public static Vector3 Plane(KwSkinBox box, float stand) => box.Centre + box.Out * (Thick / 2f - stand);

        // The box's ways: along and up, one of them the stretch's own and the other square to it, and out (turned to the
        // stretch's way out); false when they are not found.
        private static bool Axes(Vector3 along, Vector3 rise, Vector3 outward, bool upright, out Vector3 u, out Vector3 v, out Vector3 n)
        {
            (u, v, n) = (Vector3.zero, Vector3.zero, Vector3.zero);
            Vector3 first = upright ? rise : along, second = upright ? along : rise;
            if (first.sqrMagnitude < 1e-8f)
                return false;
            first.Normalize();
            second -= first * Vector3.Dot(second, first);
            if (second.sqrMagnitude < 1e-8f)
                return false;
            second.Normalize();
            (u, v) = upright ? (second, first) : (first, second);
            n = Vector3.Cross(u, v);
            n = Vector3.Dot(n, outward) < 0f ? -n : n;
            return true;
        }

        // A face over its stretch by the margin, and never less than Least along or up.
        private static Rect Grown(Rect r)
        {
            float w = Mathf.Max(r.width + 2f * Margin, Least), h = Mathf.Max(r.height + 2f * Margin, Least);
            return new Rect(r.center.x - w / 2f, r.center.y - h / 2f, w, h);
        }
    }

    /// <summary>A four-cornered outline seen on a box's face (<see cref="KwSkinFit.Over"/>), its corners in order round it. Pure.</summary>
    public readonly struct KwSkinQuad2
    {
        private readonly Vector2 p0, p1, p2, p3;

        public KwSkinQuad2(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) => (this.p0, this.p1, this.p2, this.p3) = (p0, p1, p2, p3);

        /// <summary>A point's distance outside it (0 inside).</summary>
        public float Outside(Vector2 p)
        {
            bool inside = false;
            float near = float.MaxValue;
            Edge(p, p0, p1, ref inside, ref near);
            Edge(p, p1, p2, ref inside, ref near);
            Edge(p, p2, p3, ref inside, ref near);
            Edge(p, p3, p0, ref inside, ref near);
            return inside ? 0f : near;
        }

        // One edge: the crossings of a line to the point's right, and the point's distance from it.
        private static void Edge(Vector2 p, Vector2 a, Vector2 b, ref bool inside, ref float near)
        {
            Vector2 ab = b - a;
            if ((a.y > p.y) != (b.y > p.y) && p.x < ab.x * (p.y - a.y) / ab.y + a.x)
                inside = !inside;
            float t = ab.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            near = Mathf.Min(near, (a + ab * t - p).magnitude);
        }
    }
}
