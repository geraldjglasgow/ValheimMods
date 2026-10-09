using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// The leggings themselves: every covered body triangle again, each corner pushed the look's thickness
    /// (<see cref="LegStyle.Thickness"/>) out along the body's outward normal with the body vertex's own skin weights, so
    /// they bend exactly as the skin under them did. The look's texture round the hips and legs; its leather on the band
    /// the belt lies on. And a lip from their open edges (the waist, the lower end) back to the body's surface, drawn from
    /// both sides, so nobody sees in under them. A look worn under the boots sinks close to the skin inside them
    /// (<see cref="Off"/>), so the boots' wraps cover it everywhere.
    /// </summary>
    internal static class FittedShell
    {
        // Triangles whose middle is above this lie under the belt and wear its leather.
        private const float LeatherFrom = Belt.Bottom - 0.01f;

        // Under the boots: this far off the skin, sinking from just above the boots' top over this depth.
        private const float Sunk = 0.002f;
        private const float SinkAbove = 0.01f;
        private const float SinkDepth = 0.05f;

        public static void Build(LegStyle style, BodySurface body, bool[] covered, Unwrap unwrap, FittedMesh mesh, float bootTop)
        {
            var context = new Context(style, body, unwrap, mesh, bootTop, new Dictionary<long, int>());
            for (int t = 0; t < covered.Length; t++)
            {
                if (covered[t])
                    Triangle(context, t);
            }
        }

        /// <summary>How far out a corner at <paramref name="height"/> stands: the look's thickness, or sunk under boots it is worn under.</summary>
        public static float Off(LegStyle style, float bootTop, float height)
        {
            if (!style.UnderBoots || bootTop <= LegRegion.Ankle)
                return style.Thickness;
            return Mathf.Lerp(style.Thickness, Sunk, Mathf.Clamp01((bootTop + SinkAbove - height) / SinkDepth));
        }

        /// <summary>The lip along the covered region's open edges (an edge one covered triangle uses, seams welded).</summary>
        public static void Hems(LegStyle style, BodySurface body, bool[] covered, FittedMesh mesh, float bootTop)
        {
            Vector2 uv = FittedAtlas.Region(style.Leather, 0.5f, 36f / style.Leather.height, false);
            foreach ((int a, int b) in OpenEdges(body, covered))
            {
                int outA = mesh.Add(body.Out(a, Off(style, bootTop, body.Rest[a].y)), body.Outward[a], uv, body.Weights[a]);
                int outB = mesh.Add(body.Out(b, Off(style, bootTop, body.Rest[b].y)), body.Outward[b], uv, body.Weights[b]);
                int skinA = mesh.Add(body.Vertices[a], body.Outward[a], uv, body.Weights[a]);
                int skinB = mesh.Add(body.Vertices[b], body.Outward[b], uv, body.Weights[b]);
                mesh.Both(outA, outB, skinB);
                mesh.Both(outA, skinB, skinA);
            }
        }

        private static void Triangle(Context c, int t)
        {
            BodySurface body = c.Body;
            int[] corners = { body.Triangles[3 * t], body.Triangles[3 * t + 1], body.Triangles[3 * t + 2] };
            Vector3[] rest = { body.Rest[corners[0]], body.Rest[corners[1]], body.Rest[corners[2]] };
            Vector3 middle = (rest[0] + rest[1] + rest[2]) / 3f;
            int part = Unwrap.Part(middle);
            float[] angles = c.Unwrap.Angles(part, rest);
            bool leather = middle.y > LeatherFrom;
            var index = new int[3];
            for (int i = 0; i < 3; i++)
                index[i] = Corner(c, corners[i], part, angles[i], leather);
            c.Mesh.Triangle(index[0], index[1], index[2]);
        }

        /// <summary>One vertex per body vertex, part, side of the seam and texture (field or leather), shared by its triangles.</summary>
        private static int Corner(Context c, int v, int part, float angle, bool leather)
        {
            long key = ((long)v << 8) | ((long)part << 4) | (angle >= Unwrap.Turn ? 4L : 0L) | (leather ? 1L : 0L);
            if (c.Made.TryGetValue(key, out int index))
                return index;
            BodySurface body = c.Body;
            float down = Mathf.Clamp01((LegRegion.Waist - body.Rest[v].y) / (LegRegion.Waist - LeatherFrom + 0.04f));
            Vector2 uv = leather
                ? FittedAtlas.Region(c.Style.Leather, angle / Unwrap.Turn, (4f + down * 14f) / c.Style.Leather.height, false)
                : FittedAtlas.Field(c.Style, c.Unwrap.Radius(part), angle, body.Rest[v].y);
            index = c.Mesh.Add(body.Out(v, Off(c.Style, c.BootTop, body.Rest[v].y)), body.Outward[v], uv, body.Weights[v]);
            c.Made[key] = index;
            return index;
        }

        private static List<(int, int)> OpenEdges(BodySurface body, bool[] covered)
        {
            var uses = new Dictionary<(Vector3Int, Vector3Int), int>();
            var ends = new Dictionary<(Vector3Int, Vector3Int), (int, int)>();
            for (int t = 0; t < covered.Length; t++)
            {
                for (int e = 0; covered[t] && e < 3; e++)
                {
                    int a = body.Triangles[3 * t + e], b = body.Triangles[3 * t + (e + 1) % 3];
                    var key = Edge(body.Place(a), body.Place(b));
                    uses[key] = uses.TryGetValue(key, out int n) ? n + 1 : 1;
                    ends[key] = (a, b);
                }
            }
            var open = new List<(int, int)>();
            foreach (KeyValuePair<(Vector3Int, Vector3Int), int> use in uses)
            {
                if (use.Value == 1)
                    open.Add(ends[use.Key]);
            }
            return open;
        }

        private static (Vector3Int, Vector3Int) Edge(Vector3Int p, Vector3Int q)
        {
            bool first = p.x != q.x ? p.x < q.x : p.y != q.y ? p.y < q.y : p.z < q.z;
            return first ? (p, q) : (q, p);
        }

        private readonly struct Context
        {
            public Context(LegStyle style, BodySurface body, Unwrap unwrap, FittedMesh mesh, float bootTop, Dictionary<long, int> made)
            {
                Style = style;
                Body = body;
                Unwrap = unwrap;
                Mesh = mesh;
                BootTop = bootTop;
                Made = made;
            }

            public LegStyle Style { get; }
            public BodySurface Body { get; }
            public Unwrap Unwrap { get; }
            public FittedMesh Mesh { get; }
            public float BootTop { get; }
            public Dictionary<long, int> Made { get; }
        }
    }
}
