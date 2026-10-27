using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The build order's last layer, cut from a hull piece or an upper level so it fits: a railing (a cap on the top
    /// edge, posts, a top rail 0.7 m up) that snaps onto the piece's top corners, on both sides of the hull or on one
    /// (<see cref="KwRailText.Sides"/>: the part's left, kit +y, or its right). Its posts stand at its stations about a
    /// metre apart, as railings were first built, or evenly as far apart as its text says (<see cref="KwRailText"/>), one
    /// at each end. The game's plank wood, tiered or not (<see cref="KwRailLooks"/>): its planar UVs are made for it.
    /// </summary>
    public static class KwRailing
    {
        public const float RailHeight = 0.7f;

        /// <summary>The cap's top over the edge, the top rail's depth, a rail's half width and a post's half width.</summary>
        public const float CapTop = 0.12f, TopRail = 0.08f, Half = 0.06f, Post = 0.05f;

        // Posts at the stations stand at least this far apart (railings as first built).
        private const float StationGap = 0.9f;

        /// <summary>A railing along a skin's top edge: posts that far apart (0: at its stations), on these sides (<see cref="KwRailText.Both"/> as before).</summary>
        public static KwDef Railing(KwDef skin, float spacing = 0f, string sides = KwRailText.Both)
        {
            KwDef def = new KwDef("KW_rail_" + skin.Name.Substring(3), "Railing on " + skin.Label, "railing")
                { Wood = true, Stations = skin.Stations };
            def.Shape = b => RailShape(b, skin.Stations, spacing, sides);
            def.Pivot = new Vector2(skin.Stations[0].X, skin.Stations.Min(st => Edge(st).y));
            foreach (Station end in new[] { skin.Stations[0], skin.Stations[skin.Stations.Count - 1] })
            {
                Vector2 e = Edge(end);
                foreach (float side in Drawn(sides))
                    def.Snaps.Add(("Rail end", new Vector3(end.X, side * e.x, e.y)));
            }
            return def;
        }

        /// <summary>The sides a railing stands on, +1 its left (kit +y) and -1 its right, left first.</summary>
        public static List<float> Drawn(string sides) => new[] { 1f, -1f }.Where(side => KwRailText.Has(sides, side)).ToList();

        private static Vector2 Edge(Station s) => s.Half[s.Half.Length - 1];

        private static void RailShape(MeshBuilder b, List<Station> st, float spacing, string sides)
        {
            List<float> posts = PostsAlong(st, spacing);
            foreach (float side in Drawn(sides))
            {
                for (int s = 0; s + 1 < st.Count; s++)
                {
                    Vector2 a = Edge(st[s]), c = Edge(st[s + 1]);
                    Solid.Hexa(b, Beam(st[s].X, st[s + 1].X, side * a.x, side * c.x, a.y, c.y, 0f, CapTop));
                    Solid.Hexa(b, Beam(st[s].X, st[s + 1].X, side * a.x, side * c.x, a.y, c.y, RailHeight, RailHeight + TopRail));
                }
                foreach (float x in posts)
                    PostAt(b, st, x, side);
            }
        }

        // A post from the cap up to the top rail, at x along one side (kept just inside the railing's ends).
        private static void PostAt(MeshBuilder b, List<Station> st, float x, float side)
        {
            Vector2 e = EdgeAt(st, x);
            float y = side * e.x, at = Mathf.Clamp(x, st[0].X + Post, st[st.Count - 1].X - Post);
            Solid.Box(b, new Vector3(at - Post, y - Post, e.y + CapTop), new Vector3(at + Post, y + Post, e.y + RailHeight));
        }

        /// <summary>
        /// Where along the ship a railing's posts stand: at its stations at least 0.9 m apart and always at its last (spacing 0,
        /// as railings were first built), or evenly no further apart than the spacing, one at each end.
        /// </summary>
        public static List<float> PostsAlong(List<Station> st, float spacing)
        {
            List<float> posts = new List<float>();
            if (st.Count == 0)
                return posts;
            float first = st[0].X, length = st[st.Count - 1].X - first;
            if (spacing > 0f)
            {
                int gaps = Mathf.Max(1, Mathf.CeilToInt(length / spacing - 0.001f));
                for (int i = 0; i <= gaps; i++)
                    posts.Add(first + length * i / gaps);
                return posts;
            }
            float last = float.MinValue;
            for (int s = 0; s < st.Count; s++)
            {
                if (st[s].X - last < StationGap && s != st.Count - 1)
                    continue;
                posts.Add(last = st[s].X);
            }
            return posts;
        }

        /// <summary>The top edge (half width, height) at x along the stations, between the two either side.</summary>
        public static Vector2 EdgeAt(List<Station> st, float x)
        {
            for (int s = 0; s + 1 < st.Count; s++)
            {
                if (x > st[s + 1].X && s + 2 < st.Count)
                    continue;
                float run = st[s + 1].X - st[s].X;
                float t = run > 0.0001f ? Mathf.Clamp01((x - st[s].X) / run) : 0f;
                return Vector2.Lerp(Edge(st[s]), Edge(st[s + 1]), t);
            }
            return Edge(st[0]);
        }

        // A thin beam along the edge between two stations, at a height band above the edge.
        private static Vector3[] Beam(float x0, float x1, float y0, float y1, float z0, float z1, float low, float high) => new[]
        {
            new Vector3(x0, y0 - Half, z0 + low), new Vector3(x1, y1 - Half, z1 + low), new Vector3(x1, y1 + Half, z1 + low), new Vector3(x0, y0 + Half, z0 + low),
            new Vector3(x0, y0 - Half, z0 + high), new Vector3(x1, y1 - Half, z1 + high), new Vector3(x1, y1 + Half, z1 + high), new Vector3(x0, y0 + Half, z0 + high),
        };
    }
}
