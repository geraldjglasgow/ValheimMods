using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A strake's convex piece as its fan is drawn (<see cref="MeshBuilder"/>: triangles from the mean of its corners), for
    /// the points an opening's cut puts on it (<see cref="KwOpeningRing"/>): a point of it, given where it lies in the
    /// piece's (along, up), as the triangle it lies in has it on the board's outside face and on its inside face, its
    /// measures blended the same way, and its place and way out worked back from the two faces (the board stands off its
    /// line by its proud there, <see cref="KwStrakes.Proud(KwGirthPoint, int)"/>). So every cut point lies on the faces the
    /// whole piece is drawn with, and one on the piece's edge on that edge's straight line on both faces, wherever the
    /// piece beside it has its own corners. Pure.
    /// </summary>
    public sealed class KwPieceFan
    {
        private const float OnEdge = 1e-5f;

        private readonly List<KwGirthPoint> corners;
        private readonly List<Mix> faces = new List<Mix>();
        private readonly Mix centre;
        private readonly int strake;

        /// <summary>The piece of strake k (a band over it: the strake it lies on).</summary>
        public KwPieceFan(List<KwGirthPoint> corners, int k)
        {
            (this.corners, strake) = (corners, k);
            Mix sum = default;
            foreach (KwGirthPoint p in corners)
            {
                Mix m = new Mix(p.At + p.Out * KwStrakes.Proud(p, k), p.At - p.Out * KwPlanks.Thick, p);
                faces.Add(m);
                sum = sum.Plus(m.Times(1f / corners.Count));
            }
            centre = sum;
        }

        /// <summary>One of the piece's own corners.</summary>
        public KwGirthPoint Corner(int i) => corners[i];

        /// <summary>The piece's point at (along, up), from the fan triangle it lies most inside (on an edge, its corners' blend).</summary>
        public KwGirthPoint At(Vector2 p)
        {
            float best = float.MinValue;
            (int i, float wa, float wb) = (0, 0f, 0f);
            for (int k = 0; k < corners.Count; k++)
            {
                Vector2 a = Flat(corners[k].At), b = Flat(corners[(k + 1) % corners.Count].At), c = Flat(centre.At);
                float d = Cross(a - c, b - c);
                if (Mathf.Abs(d) < 1e-12f)
                    continue;
                float sa = Cross(p - c, b - c) / d, sb = Cross(a - c, p - c) / d;
                float least = Mathf.Min(1f - sa - sb, Mathf.Min(sa, sb));
                if (least > best)
                    (best, i, wa, wb) = (least, k, sa, sb);
            }
            Mix ma = faces[i], mb = faces[(i + 1) % corners.Count];
            if (Mathf.Abs(1f - wa - wb) < OnEdge && wa + wb > 1e-6f)
                return Lift(ma.Toward(mb, wb / (wa + wb)));
            return Lift(centre.Times(1f - wa - wb).Plus(ma.Times(wa)).Plus(mb.Times(wb)));
        }

        // A blend of the faces as a strake point: its way out from its inside face to its outside face over the board's
        // depth there, its place on its line that far in from the outside face.
        private KwGirthPoint Lift(Mix m)
        {
            KwGirthPoint measures = new KwGirthPoint(Vector3.zero, Vector3.up, m.Girth, m.Along, m.Sheer, m.Keel);
            float proud = KwStrakes.Proud(measures, strake);
            Vector3 way = (m.Outside - m.Inside) / (proud + KwPlanks.Thick);
            return new KwGirthPoint(m.Outside - way * proud, way, m.Girth, m.Along, m.Sheer, m.Keel);
        }

        private static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>A point of the piece as both faces have it, its place on its line, and its measures, to blend.</summary>
        private readonly struct Mix
        {
            public Mix(Vector3 outside, Vector3 inside, KwGirthPoint p) : this(outside, inside, p.At, p.Girth, p.Along, p.Sheer, p.Keel) { }

            private Mix(Vector3 outside, Vector3 inside, Vector3 at, float girth, float along, float sheer, float keel) =>
                (Outside, Inside, At, Girth, Along, Sheer, Keel) = (outside, inside, at, girth, along, sheer, keel);

            public Vector3 Outside { get; }
            public Vector3 Inside { get; }
            public Vector3 At { get; }
            public float Girth { get; }
            public float Along { get; }
            public float Sheer { get; }
            public float Keel { get; }

            public Mix Times(float f) => new Mix(Outside * f, Inside * f, At * f, Girth * f, Along * f, Sheer * f, Keel * f);

            public Mix Plus(Mix m) => new Mix(Outside + m.Outside, Inside + m.Inside, At + m.At, Girth + m.Girth, Along + m.Along, Sheer + m.Sheer, Keel + m.Keel);

            // The point a share f along an edge to m, a measure both ends share kept exactly (a cut along the sheer or a
            // strake's edge still reads as on it).
            public Mix Toward(Mix m, float f) => new Mix(Vector3.LerpUnclamped(Outside, m.Outside, f), Vector3.LerpUnclamped(Inside, m.Inside, f),
                Vector3.LerpUnclamped(At, m.At, f), Girth + (m.Girth - Girth) * f, Along + (m.Along - Along) * f, Sheer + (m.Sheer - Sheer) * f, Keel + (m.Keel - Keel) * f);
        }
    }
}
