using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hole's broken edge (<see cref="KwHoleCut"/>): its outline walked in steps of <see cref="Step"/>, and at each step
    /// the part's own faces found straight along the hole's normal (the outermost and the innermost, from the triangles the
    /// hole reached, <see cref="KwRimProbe"/>); between two steps that both cross wood a band of the same wood runs through
    /// the board from its outside face (a lip past it, over a painted band) to its inside, drawn both ways like every face
    /// of the mod's, so the cut shows the board's broken end and nothing looks through between the faces. At the outline's
    /// splintered notches (<see cref="KwHoleShape.Splinters"/>) a splinter juts from the broken end into the hole, along the
    /// grain and bent in through the hull as a shot from outside leaves it. In the part's own wood: each band's and
    /// splinter's UVs are those of the outside face where it starts. Pure.
    /// </summary>
    public static class KwHoleRim
    {
        private const float Step = 0.035f, Lip = 0.006f, Foot = 0.002f;

        public static void Add(KwHoleMesh mesh, KwHoleMesh cut, KwHoleShape hole, List<(int Sub, int At)> touched)
        {
            if (touched.Count == 0)
                return;
            KwRimProbe probe = new KwRimProbe(mesh, hole, touched);
            List<KwRimSample> samples = Samples(hole, probe);
            List<int> into = cut.Subs[touched[0].Sub];
            for (int j = 0; j < samples.Count; j++)
                Band(cut, into, hole, samples[j], samples[(j + 1) % samples.Count]);
            foreach (int corner in hole.Splinters)
                KwHoleSplinter.Add(cut, into, hole, samples.Find(s => s.Corner == corner));
        }

        // The outline in steps, each edge from its first corner, each step probed.
        private static List<KwRimSample> Samples(KwHoleShape hole, KwRimProbe probe)
        {
            List<KwRimSample> samples = new List<KwRimSample>();
            List<Vector2> outline = hole.Outline;
            for (int k = 0; k < outline.Count; k++)
            {
                Vector2 a = outline[k], b = outline[(k + 1) % outline.Count];
                int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / Step));
                for (int i = 0; i < steps; i++)
                    samples.Add(probe.At(Vector2.Lerp(a, b, i / (float)steps), i == 0 ? k : -1));
            }
            return samples;
        }

        // A band of the board's broken end between two steps, facing into the hole and out of it.
        private static void Band(KwHoleMesh cut, List<int> into, KwHoleShape hole, KwRimSample s0, KwRimSample s1)
        {
            if (!s0.Solid || !s1.Solid)
                return;
            Vector2 run = s1.At - s0.At;
            Vector3 inward = (hole.Along * -run.y + hole.Up * run.x).normalized;
            Vector3[] corners =
            {
                hole.Point(s0.At, s0.Outer + Lip), hole.Point(s0.At, s0.Inner - Foot),
                hole.Point(s1.At, s1.Inner - Foot), hole.Point(s1.At, s1.Outer + Lip),
            };
            Vector2[] uvs = { s0.Uv, s0.Uv + new Vector2(0.004f, 0.004f), s1.Uv + new Vector2(0.004f, 0.004f), s1.Uv };
            Polygon(cut, into, corners, uvs, inward);
            Polygon(cut, into, corners, uvs, -inward);
        }

        /// <summary>A flat polygon facing one way (its corners' order turned to suit), with that normal and these UVs.</summary>
        public static void Polygon(KwHoleMesh cut, List<int> into, Vector3[] corners, Vector2[] uvs, Vector3 facing)
        {
            bool turned = Vector3.Dot(Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]), facing) < 0f;
            Vector3 along = (corners[1] - corners[0]).normalized;
            Vector4 tangent = new Vector4(along.x, along.y, along.z, 1f);
            int first = cut.Count;
            for (int i = 0; i < corners.Length; i++)
            {
                int c = turned ? corners.Length - 1 - i : i;
                cut.Add(new KwHoleVertex(corners[c], facing, tangent, uvs[c], Color.white));
            }
            for (int i = 1; i + 1 < corners.Length; i++)
            {
                into.Add(first);
                into.Add(first + i);
                into.Add(first + i + 1);
            }
        }
    }

    /// <summary>One step round a hole's outline: where, which corner (-1 none), and the part's faces there.</summary>
    public readonly struct KwRimSample
    {
        public KwRimSample(Vector2 at, int corner, float outer, float inner, Vector2 uv, bool found) =>
            (At, Corner, Outer, Inner, Uv, Found) = (at, corner, outer, inner, uv, found);

        public Vector2 At { get; }
        public int Corner { get; }
        public float Outer { get; }
        public float Inner { get; }
        public Vector2 Uv { get; }
        public bool Found { get; }

        /// <summary>Whether wood of some thickness lies across the step (a board, not a lone skin).</summary>
        public bool Solid => Found && Outer - Inner >= 0.004f;
    }

    /// <summary>
    /// The triangles a hole reached, laid flat in its plane, and where its normal through a point of the plane meets them:
    /// the outermost and innermost depths within the hole's cut, and the outermost face's UV there. Pure.
    /// </summary>
    public sealed class KwRimProbe
    {
        private readonly List<(Vector2 A, Vector2 B, Vector2 C, Vector3 Depth, Vector2 UvA, Vector2 UvB, Vector2 UvC)> flat =
            new List<(Vector2, Vector2, Vector2, Vector3, Vector2, Vector2, Vector2)>();

        public KwRimProbe(KwHoleMesh mesh, KwHoleShape hole, List<(int Sub, int At)> touched)
        {
            bool uvs = mesh.Uvs.Count == mesh.Count;
            foreach ((int sub, int at) in touched)
            {
                List<int> tris = mesh.Subs[sub];
                int i = tris[at], j = tris[at + 1], k = tris[at + 2];
                Vector2 a = hole.Flat(mesh.Points[i]), b = hole.Flat(mesh.Points[j]), c = hole.Flat(mesh.Points[k]);
                if (Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 1e-9f)
                    continue;
                Vector3 depth = new Vector3(hole.Depth(mesh.Points[i]), hole.Depth(mesh.Points[j]), hole.Depth(mesh.Points[k]));
                flat.Add((a, b, c, depth, uvs ? mesh.Uvs[i] : Vector2.zero, uvs ? mesh.Uvs[j] : Vector2.zero, uvs ? mesh.Uvs[k] : Vector2.zero));
            }
        }

        public KwRimSample At(Vector2 p, int corner)
        {
            (float outer, float inner, Vector2 uv, bool found) = (float.MinValue, float.MaxValue, Vector2.zero, false);
            foreach (var t in flat)
            {
                if (!Weights(p, t.A, t.B, t.C, out Vector3 w))
                    continue;
                float d = Vector3.Dot(w, t.Depth);
                if (d > KwHoleCut.Outside + 0.03f || d < -KwHoleCut.Inside - 0.03f)
                    continue;
                if (d > outer)
                    (outer, uv) = (d, t.UvA * w.x + t.UvB * w.y + t.UvC * w.z);
                (inner, found) = (Mathf.Min(inner, d), true);
            }
            return new KwRimSample(p, corner, outer, inner, uv, found);
        }

        // A point's barycentric weights in a flat triangle; false outside it (a hair's slack).
        private static bool Weights(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 w)
        {
            float det = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            float wa = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / det;
            float wb = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / det;
            w = new Vector3(wa, wb, 1f - wa - wb);
            return w.x >= -1e-4f && w.y >= -1e-4f && w.z >= -1e-4f;
        }
    }
}
