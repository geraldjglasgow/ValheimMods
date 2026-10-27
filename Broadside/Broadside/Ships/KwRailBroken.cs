using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's railing drawn with stretches broken (<see cref="KwShipRails"/>), in the railing's own timbers and
    /// UVs as its look is built (<see cref="KwRailing"/> for a hull railing, <see cref="KwRoofRailShape"/> for a roof
    /// railing): its cap kept whole along the edge; its top rail gone over each run of broken stretches but for a broken
    /// stub left past each post at its ends, splintered at the break; a post whose stretches either side are all broken
    /// snapped to a splintered stub over the cap. Pure but for the mesh builder.
    /// </summary>
    public static class KwRailBroken
    {
        private const float Stub = 0.1f, Splinter = 0.08f, Snapped = 0.22f;

        /// <summary>The railing's look with its broken stretches (parts) broken; null when its text does not read.</summary>
        public static Mesh? Build(KwBpPiece piece, KwKeelLines keels, ICollection<int> broken) =>
            Def(piece, keels, broken) is KwDef def ? KwMesh.Build(def) : null;

        /// <summary>The broken railing's definition, its shape drawn into a mesh builder; null when its text does not read.</summary>
        public static KwDef? Def(KwBpPiece piece, KwKeelLines keels, ICollection<int> broken)
        {
            string saved = KwShipParts.Saved(piece);
            KwDef def = new KwDef("KW_gen", "Railing", "railing") { Wood = true };
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
                def.Shape = b => Roof(b, roof, broken);
            else if (KwSpan.TryParse(saved, out KwSpan span))
            {
                List<Station> st = KwRailStretches.Stations(piece, span, keels);
                (float spacing, string sides) = (KwRailText.Spacing(span.Rail), KwRailText.Sides(span.Rail));
                (def.Stations, def.Shape) = (st, b => Hull(b, st, spacing, sides, broken));
            }
            else
                return null;
            return def;
        }

        private static void Hull(MeshBuilder b, List<Station> st, float spacing, string sides, ICollection<int> broken)
        {
            List<float> posts = KwRailing.PostsAlong(st, spacing), drawn = KwRailing.Drawn(sides);
            for (int r = 0; r < drawn.Count; r++)
            {
                List<Vector2> gone = Gone(posts, broken, r, null, false);
                for (int s = 0; s + 1 < st.Count; s++)
                    HullSegment(b, st, s, drawn[r], gone);
                foreach (Vector2 g in gone)
                    HullBreaks(b, st, drawn[r], g, posts);
                for (int k = 0; k < posts.Count; k++)
                    HullPost(b, st, posts[k], drawn[r], Snaps(posts.Count - 1, false, broken, r, k, null));
            }
        }

        // One station segment of one side: its cap whole, its top rail less the gone stretches.
        private static void HullSegment(MeshBuilder b, List<Station> st, int s, float side, List<Vector2> gone)
        {
            float x0 = st[s].X, x1 = st[s + 1].X;
            Vector2 a = Edge(st[s]), c = Edge(st[s + 1]);
            Solid.Hexa(b, Beam(x0, x1, side * a.x, side * c.x, a.y, c.y, 0f, KwRailing.CapTop));
            foreach (Vector2 keep in Left(x0, x1, gone))
            {
                Vector2 e0 = KwRailing.EdgeAt(st, keep.x), e1 = KwRailing.EdgeAt(st, keep.y);
                Solid.Hexa(b, Beam(keep.x, keep.y, side * e0.x, side * e1.x, e0.y, e1.y, KwRailing.RailHeight, KwRailing.RailHeight + KwRailing.TopRail));
            }
        }

        // The top rail's broken ends either side of a gone run, each a splinter into the gap (none over a snapped post).
        private static void HullBreaks(MeshBuilder b, List<Station> st, float side, Vector2 gone, List<float> posts)
        {
            foreach ((float x, float way) in new[] { (gone.x, 1f), (gone.y, -1f) })
            {
                if (AtPost(posts, x))
                    continue;
                Vector2 e = KwRailing.EdgeAt(st, x);
                Vector3 end = new Vector3(x, side * e.x, e.y + KwRailing.RailHeight + KwRailing.TopRail / 2f);
                Splintered(b, end, new Vector3(way, 0f, 0f), new Vector3(0f, 1f, 0f), KwRailing.Half, KwRailing.TopRail / 2f);
            }
        }

        // A post as KwRailing stands it, or snapped to a stub.
        private static void HullPost(MeshBuilder b, List<Station> st, float x, float side, bool snapped)
        {
            Vector2 e = KwRailing.EdgeAt(st, x);
            float y = side * e.x, at = Mathf.Clamp(x, st[0].X + KwRailing.Post, st[st.Count - 1].X - KwRailing.Post), p = KwRailing.Post;
            float top = e.y + (snapped ? KwRailing.CapTop + Snapped : KwRailing.RailHeight);
            Solid.Box(b, new Vector3(at - p, y - p, e.y + KwRailing.CapTop), new Vector3(at + p, y + p, top));
            if (snapped)
                Splintered(b, new Vector3(at, y, top), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f), p, p);
        }

        private static void Roof(MeshBuilder b, KwRoofRail rail, ICollection<int> broken)
        {
            List<KwRoofRun> runs = KwRailStretches.Runs(rail);
            for (int r = 0; r < runs.Count; r++)
            {
                KwRoofRun run = runs[r];
                List<float> posts = KwRoofRailShape.Posts(run, rail.Spacing), stretch = KwRailStretches.Posts(run, rail.Spacing);
                bool closed = stretch.Count > posts.Count;
                List<Vector2> gone = Gone(stretch, broken, r, run, closed);
                for (int i = 0; i + 1 < run.Points.Count; i++)
                    RoofEdge(b, run, i, gone);
                foreach (Vector2 g in gone)
                    RoofBreaks(b, run, g, stretch);
                for (int k = 0; k < posts.Count; k++)
                    RoofPost(b, run, posts[k], Snaps(stretch.Count - 1, closed, broken, r, k, run, stretch));
            }
        }

        // One edge of a roof run: its cap round its gaps as KwRoofRailShape lays it, its top rail less the gone stretches.
        private static void RoofEdge(MeshBuilder b, KwRoofRun run, int i, List<Vector2> gone)
        {
            foreach (Vector2 piece in run.Pieces(i))
            {
                RoofBeam(b, run, i, piece, 0f, KwRailing.CapTop);
                foreach (Vector2 keep in Left(piece.x, piece.y, gone))
                    RoofBeam(b, run, i, keep, KwRailing.RailHeight, KwRailing.RailHeight + KwRailing.TopRail);
            }
        }

        // A beam along edge i over a stretch of it, reaching half a rail past the edge's own ends (as at a corner).
        private static void RoofBeam(MeshBuilder b, KwRoofRun run, int i, Vector2 piece, float low, float high)
        {
            float from = piece.x <= run.Along[i] + 0.001f ? -KwRailing.Half : 0f, to = piece.y >= run.Along[i + 1] - 0.001f ? KwRailing.Half : 0f;
            Vector2 a = run.At(i, piece.x) + run.Way(i) * from, c = run.At(i, piece.y) + run.Way(i) * to, w = run.Inward(i) * 2f * KwRailing.Half;
            Solid.Hexa(b, new[] { V(a, low), V(c, low), V(c + w, low), V(a + w, low), V(a, high), V(c, high), V(c + w, high), V(a + w, high) });
        }

        private static void RoofBreaks(MeshBuilder b, KwRoofRun run, Vector2 gone, List<float> posts)
        {
            foreach ((float at, float way) in new[] { (gone.x, 1f), (gone.y, -1f) })
            {
                if (AtPost(posts, at))
                    continue;
                int i = run.Segment(Mathf.Min(at, run.Length));
                Vector2 p = run.At(i, Mathf.Min(at, run.Length)) + run.Inward(i) * KwRailing.Half, along = run.Way(i) * way, across = run.Inward(i);
                Vector3 end = V(p, KwRailing.RailHeight + KwRailing.TopRail / 2f);
                Splintered(b, end, new Vector3(along.x, along.y, 0f), new Vector3(across.x, across.y, 0f), KwRailing.Half, KwRailing.TopRail / 2f);
            }
        }

        // A roof post as KwRoofRailShape stands it, or snapped to a stub.
        private static void RoofPost(MeshBuilder b, KwRoofRun run, float at, bool snapped)
        {
            int i = run.Segment(at);
            Vector2 way = run.Way(i) * KwRailing.Post, side = run.Inward(i) * KwRailing.Post, centre = run.At(i, at) + run.Inward(i) * KwRailing.Half;
            Vector2[] c = { centre - way - side, centre + way - side, centre + way + side, centre - way + side };
            float top = snapped ? KwRailing.CapTop + Snapped : KwRailing.RailHeight;
            Solid.Hexa(b, new[] { V(c[0], KwRailing.CapTop), V(c[1], KwRailing.CapTop), V(c[2], KwRailing.CapTop), V(c[3], KwRailing.CapTop), V(c[0], top), V(c[1], top), V(c[2], top), V(c[3], top) });
            if (snapped)
                Splintered(b, V(centre, top), new Vector3(0f, 0f, 1f), new Vector3(way.x, way.y, 0f).normalized, KwRailing.Post, KwRailing.Post);
        }

        // Each run of broken stretches along a run (whole, between its end posts), less a stub at each end whose post still
        // stands (none over a snapped post).
        private static List<Vector2> Gone(List<float> posts, ICollection<int> broken, int run, KwRoofRun? roof, bool closed)
        {
            List<(Vector2 Span, int From, int To)> gone = new List<(Vector2, int, int)>();
            for (int k = 0; k + 1 < posts.Count; k++)
            {
                if (!broken.Contains(KwRailStretches.Part(run, k)) || (roof != null && Gapped(roof, posts[k], posts[k + 1])))
                    continue;
                if (gone.Count > 0 && gone[gone.Count - 1].To == k)
                    gone[gone.Count - 1] = (new Vector2(gone[gone.Count - 1].Span.x, posts[k + 1]), gone[gone.Count - 1].From, k + 1);
                else
                    gone.Add((new Vector2(posts[k], posts[k + 1]), k, k + 1));
            }
            int count = posts.Count - 1;
            return gone.ConvertAll(g => new Vector2(g.Span.x + (Snaps(count, closed, broken, run, g.From, roof, posts) ? 0f : Stub),
                g.Span.y - (Snaps(count, closed, broken, run, closed && g.To == count ? 0 : g.To, roof, posts) ? 0f : Stub))).FindAll(g => g.y > g.x);
        }

        // Whether a break ends right at a post (snapped, no stub of rail left there).
        private static bool AtPost(List<float> posts, float at) => posts.Exists(p => Mathf.Abs(p - at) < 0.01f);

        private static bool Gapped(KwRoofRun run, float from, float to) => run.Gaps.Exists(g => g.x > from - 0.01f && g.y < to + 0.01f);

        // Whether a post's stretches either side (those that are there) are all broken.
        private static bool Snaps(int stretches, bool closed, ICollection<int> broken, int run, int k, KwRoofRun? roof, List<float>? posts = null)
        {
            bool any = false;
            foreach (int s in new[] { k - 1 < 0 && closed ? stretches - 1 : k - 1, k })
            {
                if (s < 0 || s >= stretches || (roof != null && posts != null && Gapped(roof, posts[s], posts[s + 1])))
                    continue;
                if (!broken.Contains(KwRailStretches.Part(run, s)))
                    return false;
                any = true;
            }
            return any;
        }

        // The stretches of [from, to] left once the gone ones are cut out.
        private static List<Vector2> Left(float from, float to, List<Vector2> gone)
        {
            List<Vector2> left = new List<Vector2> { new Vector2(from, to) };
            foreach (Vector2 g in gone)
            {
                List<Vector2> cut = new List<Vector2>();
                foreach (Vector2 p in left)
                {
                    if (g.x > p.x + 0.001f)
                        cut.Add(new Vector2(p.x, Mathf.Min(p.y, g.x)));
                    if (g.y < p.y - 0.001f)
                        cut.Add(new Vector2(Mathf.Max(p.x, g.y), p.y));
                }
                left = cut.FindAll(p => p.y - p.x > 0.001f);
            }
            return left;
        }

        // A splinter off a broken end: a wedge from the end's section (half sizes across and up its width side) tapering
        // to a sliver Splinter beyond it along out, its tip a little off the middle.
        private static void Splintered(MeshBuilder b, Vector3 end, Vector3 out1, Vector3 across, float halfAcross, float halfUp)
        {
            Vector3 up = Vector3.Cross(out1, across).normalized, tip = end + out1.normalized * Splinter + up * (halfUp * 0.4f);
            Vector3 a = across.normalized * halfAcross, u = up * halfUp, ta = a * 0.25f, tu = u * 0.15f;
            Solid.Hexa(b, new[] { end - a - u, tip - ta - tu, tip + ta - tu, end + a - u, end - a + u, tip - ta + tu, tip + ta + tu, end + a + u });
        }

        // A thin beam along the edge between two places, at a height band above the edge (as KwRailing's).
        private static Vector3[] Beam(float x0, float x1, float y0, float y1, float z0, float z1, float low, float high) => new[]
        {
            new Vector3(x0, y0 - KwRailing.Half, z0 + low), new Vector3(x1, y1 - KwRailing.Half, z1 + low), new Vector3(x1, y1 + KwRailing.Half, z1 + low), new Vector3(x0, y0 + KwRailing.Half, z0 + low),
            new Vector3(x0, y0 - KwRailing.Half, z0 + high), new Vector3(x1, y1 - KwRailing.Half, z1 + high), new Vector3(x1, y1 + KwRailing.Half, z1 + high), new Vector3(x0, y0 + KwRailing.Half, z0 + high),
        };

        private static Vector2 Edge(Station s) => s.Half[s.Half.Length - 1];

        private static Vector3 V(Vector2 p, float z) => new Vector3(p.x, p.y, z);
    }
}
