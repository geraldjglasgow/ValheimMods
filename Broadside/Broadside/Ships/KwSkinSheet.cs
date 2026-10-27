using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A stretch of a hull's face as the boxes on it see it (<see cref="KwHullSkin"/>): points in rows, a row a station (a
    /// section's points from the keel up on one side, or the two lowest points across the keel), every four neighbouring
    /// points a quad drawn as four triangles from its middle, as <see cref="MeshBuilder"/> fans a strake's piece; each quad
    /// with its way out of the hull. A box is held to the face near its quad (the quads round it), so one that runs on over
    /// a fold or past an edge is seen to stand off. Where the face goes on beyond the part (into the next part at a rib,
    /// or below a cabin's raised side into the planking), it is carried on past that edge, so a box running on over it is
    /// not taken for one standing off into the air. Kit axes of the part. Pure.
    /// </summary>
    public sealed class KwSkinSheet
    {
        private const float Down = 0.5f;
        private readonly Vector3[][] rows;
        private readonly Vector3[][] outs;
        private readonly KwSkinQuad[][] quads;

        public KwSkinSheet(Vector3[][] rows, System.Func<int, int, Vector3> outward, bool before, bool after, bool below = false)
        {
            (First, Last, Low) = (before && rows.Length > 1 ? 1 : 0, rows.Length - 1, below ? 1 : 0);
            this.rows = Carried(Lowered(rows, below), before, after);
            Last += First;
            Width = int.MaxValue;
            foreach (Vector3[] row in this.rows)
                Width = Mathf.Min(Width, row.Length);
            outs = new Vector3[Mathf.Max(0, this.rows.Length - 1)][];
            quads = new KwSkinQuad[outs.Length][];
            for (int i = 0; i < outs.Length; i++)
            {
                (outs[i], quads[i]) = (new Vector3[Mathf.Max(0, Width - 1)], new KwSkinQuad[Mathf.Max(0, Width - 1)]);
                for (int j = 0; j < outs[i].Length; j++)
                    (outs[i][j], quads[i][j]) = (outward(i, j - Low), new KwSkinQuad(this.rows[i][j], this.rows[i][j + 1], this.rows[i + 1][j + 1], this.rows[i + 1][j]));
            }
        }

        /// <summary>One side of a run of stations: each section's points from the keel up (1 port, -1 starboard), carried on past the edges that go on.</summary>
        public static KwSkinSheet Side(List<Station> stations, float side, bool before = false, bool after = false, bool below = false)
        {
            Vector3[][] rows = stations.ConvertAll(s => System.Array.ConvertAll(s.Half, p => new Vector3(s.X, side * p.x, p.y))).ToArray();
            int from = before ? 1 : 0;
            return new KwSkinSheet(rows, (i, j) => Outward(stations, i - from, Mathf.Max(j, 0), side), before, after, below);
        }

        /// <summary>The strip across the keel between the two sides' lowest points, facing out under the keel line.</summary>
        public static KwSkinSheet Keel(List<Station> stations, bool before = false, bool after = false)
        {
            Vector3[][] rows = stations.ConvertAll(s => new[] { new Vector3(s.X, -s.Half[0].x, s.Half[0].y), new Vector3(s.X, s.Half[0].x, s.Half[0].y) }).ToArray();
            int from = before ? 1 : 0;
            return new KwSkinSheet(rows, (i, j) => Under(stations, Mathf.Clamp(i - from, 0, stations.Count - 2)), before, after);
        }

        /// <summary>The first and last row of the part itself, and its first point up a row (the rest carried on past its edges).</summary>
        public int First { get; }
        public int Last { get; }
        public int Low { get; }

        /// <summary>How many points each row has (the least of them).</summary>
        public int Width { get; }

        /// <summary>Quad (i, j)'s way out of the hull (not a unit).</summary>
        public Vector3 Out(int i, int j) => outs[i][j];

        /// <summary>A point of quad (i, j): s of the way from row i to row i + 1, t from point j to j + 1.</summary>
        public Vector3 Point(int i, int j, float s, float t) =>
            Vector3.Lerp(Vector3.Lerp(rows[i][j], rows[i][j + 1], t), Vector3.Lerp(rows[i + 1][j], rows[i + 1][j + 1], t), s);

        /// <summary>Whether a point lies within reach of the face drawn round quad (i, j) (it first, then the quads touching it).</summary>
        public bool Near(Vector3 p, int i, int j, float reach)
        {
            if (quads[i][j].Near(p, reach))
                return true;
            for (int di = -1; di <= 1; di++)
            {
                for (int dj = -1; dj <= 1; dj++)
                {
                    int a = i + di, b = j + dj;
                    if ((di != 0 || dj != 0) && a >= 0 && a < quads.Length && b >= 0 && b < quads[a].Length && quads[a][b].Near(p, reach))
                        return true;
                }
            }
            return false;
        }

        // The rows with one carried on past either end that goes on into another part (the last gap again).
        private static Vector3[][] Carried(Vector3[][] rows, bool before, bool after)
        {
            if (rows.Length < 2)
                return rows;
            List<Vector3[]> all = new List<Vector3[]>(rows);
            if (before)
                all.Insert(0, Beyond(rows[0], rows[1]));
            if (after)
                all.Add(Beyond(rows[rows.Length - 1], rows[rows.Length - 2]));
            return all.ToArray();
        }

        private static Vector3[] Beyond(Vector3[] end, Vector3[] next)
        {
            Vector3[] row = new Vector3[Mathf.Min(end.Length, next.Length)];
            for (int j = 0; j < row.Length; j++)
                row[j] = 2f * end[j] - next[j];
            return row;
        }

        // Each row with a point carried straight down from its first, when the face goes on below it.
        private static Vector3[][] Lowered(Vector3[][] rows, bool below)
        {
            if (!below)
                return rows;
            return System.Array.ConvertAll(rows, row =>
            {
                Vector3[] lowered = new Vector3[row.Length + 1];
                System.Array.Copy(row, 0, lowered, 1, row.Length);
                lowered[0] = row[0] - new Vector3(0f, 0f, Down);
                return lowered;
            });
        }

        // Stretch j's way out between station i and the next (a gap carried past an end: the end gap's), one side.
        private static Vector3 Outward(List<Station> stations, int i, int j, float side)
        {
            i = Mathf.Clamp(i, 0, stations.Count - 2);
            return Outward(stations[i].Half, j, side) + Outward(stations[i + 1].Half, j, side);
        }

        // The way out under the keel line between station i and the next.
        private static Vector3 Under(List<Station> stations, int i) =>
            new Vector3(stations[i + 1].Half[0].y - stations[i].Half[0].y, 0f, stations[i].X - stations[i + 1].X);

        // Stretch j's way out of the hull in a section (kit, one side); zero for a stretch with no length.
        private static Vector3 Outward(Vector2[] half, int j, float side)
        {
            Vector2 run = half[j + 1] - half[j];
            if (run.sqrMagnitude < 1e-8f)
                return Vector3.zero;
            Vector2 n = new Vector2(run.y, -run.x).normalized;
            return new Vector3(0f, side * n.x, n.y);
        }
    }

    /// <summary>
    /// One quad of a <see cref="KwSkinSheet"/> as drawn: four triangles from its middle, worked out once, with a ball round
    /// them, so whether a point lies within reach of it is mostly a distance or a plane's away. Pure.
    /// </summary>
    public readonly struct KwSkinQuad
    {
        private readonly Vector3 mid, plane;
        private readonly Vector3[] corners, normals;
        private readonly float radius, bend;

        public KwSkinQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            (corners, mid, normals, radius) = (new[] { a, b, c, d }, (a + b + c + d) / 4f, new Vector3[4], 0f);
            plane = Vector3.Cross(c - a, d - b).normalized;
            bend = 0f;
            for (int k = 0; k < 4; k++)
            {
                normals[k] = Vector3.Cross(corners[k] - mid, corners[(k + 1) % 4] - mid);
                normals[k] = normals[k].sqrMagnitude > 1e-12f ? normals[k].normalized : Vector3.zero;
                radius = Mathf.Max(radius, (corners[k] - mid).magnitude);
                bend = Mathf.Max(bend, Mathf.Abs(Vector3.Dot(corners[k] - mid, plane)));
            }
        }

        /// <summary>Whether a point lies within reach of one of its triangles.</summary>
        public bool Near(Vector3 p, float reach)
        {
            if (corners == null || (p - mid).sqrMagnitude > (radius + reach) * (radius + reach))
                return false;
            if (plane != Vector3.zero && Mathf.Abs(Vector3.Dot(p - mid, plane)) > reach + bend)
                return false;
            for (int k = 0; k < 4; k++)
            {
                if (Triangle(p, corners[k], corners[(k + 1) % 4], normals[k], reach))
                    return true;
            }
            return false;
        }

        // Within reach of the triangle mid, a, b (its unit normal n): square onto it when that falls inside, else its edges.
        private bool Triangle(Vector3 p, Vector3 a, Vector3 b, Vector3 n, float reach)
        {
            float h = Vector3.Dot(p - mid, n);
            if (Mathf.Abs(h) > reach)
                return false;
            Vector3 q = p - n * h;
            if (n != Vector3.zero && Side(q, mid, a, n) && Side(q, a, b, n) && Side(q, b, mid, n))
                return true;
            return Segment(p, mid, a) <= reach || Segment(p, a, b) <= reach || Segment(p, b, mid) <= reach;
        }

        private static bool Side(Vector3 q, Vector3 a, Vector3 b, Vector3 n) => Vector3.Dot(Vector3.Cross(b - a, q - a), n) >= 0f;

        private static float Segment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - p).magnitude;
        }
    }
}
