using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A roof railing's mesh (<see cref="KwRoofRail"/>), in the hull railing's timbers (<see cref="KwRailing"/>): along every
    /// railed edge of the roof's outline a cap on the roof and a top rail 0.7 m up, each just inside the edge, and posts
    /// between them. Edges meeting end to end are one run: its corners (turns of more than 25°) and its ends get a post,
    /// and between them the posts stand evenly no further apart than the spacing. Where a ladder comes up (a gap) the cap
    /// and the rail stop, with a post either side, and no post stands in it. Kit axes, the roof's top at z = 0. Pure but for
    /// the mesh builder.
    /// </summary>
    public static class KwRoofRailShape
    {
        private const float Join = 0.05f, Corner = 25f;

        /// <summary>The railing's definition: its own shape, the plank wood's kind.</summary>
        public static KwDef Def(KwRoofRail rail) => new KwDef("KW_gen", "Railing", "railing") { Wood = true, Shape = b => Shape(b, rail) };

        public static void Shape(MeshBuilder b, KwRoofRail rail)
        {
            Vector2 middle = Middle(rail.Edges);
            foreach (List<Vector2> points in Runs(rail.Railed))
            {
                KwRoofRun run = new KwRoofRun(points, middle, rail.Gaps);
                Rails(b, run);
                foreach (float at in Posts(run, rail.Spacing))
                    Column(b, run, at);
            }
        }

        /// <summary>The railed edges chained into runs, each a line of points from end to end (a closed outline comes back to its start).</summary>
        public static List<List<Vector2>> Runs(List<KwRoofSegment> edges)
        {
            List<KwRoofSegment> left = new List<KwRoofSegment>(edges.Where(e => e.Length > 0.01f));
            List<List<Vector2>> runs = new List<List<Vector2>>();
            while (left.Count > 0)
            {
                List<Vector2> run = new List<Vector2> { left[0].A, left[0].B };
                left.RemoveAt(0);
                while (Grow(run, left, true) || Grow(run, left, false))
                {
                }
                runs.Add(run);
            }
            return runs;
        }

        // One more edge on the run's end (or its start), whichever way round it meets it; false when none does.
        private static bool Grow(List<Vector2> run, List<KwRoofSegment> left, bool atEnd)
        {
            Vector2 tip = atEnd ? run[run.Count - 1] : run[0];
            for (int i = 0; i < left.Count; i++)
            {
                bool forward = (left[i].A - tip).magnitude < Join, back = (left[i].B - tip).magnitude < Join;
                if (!forward && !back)
                    continue;
                Vector2 next = forward ? left[i].B : left[i].A;
                left.RemoveAt(i);
                if (atEnd)
                    run.Add(next);
                else
                    run.Insert(0, next);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Where along a run its posts stand: its ends and corners, evenly between them no further apart than the spacing,
        /// none in a gap and one just outside each side of it (a closed run's start and end are one post).
        /// </summary>
        public static List<float> Posts(KwRoofRun run, float spacing)
        {
            List<float> posts = new List<float>();
            List<int> corners = run.Corners(Corner);
            for (int c = 0; c + 1 < corners.Count; c++)
            {
                float from = run.Along[corners[c]], length = run.Along[corners[c + 1]] - from;
                int gaps = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(spacing, 0.1f) - 0.001f));
                for (int i = c == 0 ? 0 : 1; i <= gaps; i++)
                    posts.Add(from + length * i / gaps);
            }
            if ((run.Points[0] - run.Points[run.Points.Count - 1]).magnitude < Join)
                posts.RemoveAll(at => at > run.Length - 0.02f);
            posts.RemoveAll(at => run.Gaps.Any(g => at > g.x - KwRailing.Post && at < g.y + KwRailing.Post));
            foreach (Vector2 gap in run.Gaps)
            {
                float[] sides = { gap.x - KwRailing.Post, gap.y + KwRailing.Post };
                posts.AddRange(sides.Where(at => at > 0.02f && at < run.Length - 0.02f && posts.All(p => Mathf.Abs(p - at) > 0.1f)));
            }
            posts.Sort();
            return posts;
        }

        // The cap and the top rail along every edge of a run, cut round its gaps, reaching half a rail past a corner or an end.
        private static void Rails(MeshBuilder b, KwRoofRun run)
        {
            for (int i = 0; i + 1 < run.Points.Count; i++)
            {
                foreach (Vector2 piece in run.Pieces(i))
                {
                    float from = piece.x <= run.Along[i] + 0.001f ? -KwRailing.Half : 0f;
                    float to = piece.y >= run.Along[i + 1] - 0.001f ? KwRailing.Half : 0f;
                    Vector2 a = run.At(i, piece.x) + run.Way(i) * from, c = run.At(i, piece.y) + run.Way(i) * to;
                    Beam(b, a, c, run.Inward(i), 0f, KwRailing.CapTop);
                    Beam(b, a, c, run.Inward(i), KwRailing.RailHeight, KwRailing.RailHeight + KwRailing.TopRail);
                }
            }
        }

        // A beam from a to c (its outer face on the line, its width inward), between two heights.
        private static void Beam(MeshBuilder b, Vector2 a, Vector2 c, Vector2 inward, float low, float high)
        {
            Vector2 w = inward * 2f * KwRailing.Half;
            Vector3[] ring = { V(a, low), V(c, low), V(c + w, low), V(a + w, low), V(a, high), V(c, high), V(c + w, high), V(a + w, high) };
            Solid.Hexa(b, ring);
        }

        // A post at a place along a run, from the cap up to the top rail, square to its edge, inside the line.
        private static void Column(MeshBuilder b, KwRoofRun run, float at)
        {
            int i = run.Segment(at);
            Vector2 way = run.Way(i) * KwRailing.Post, inward = run.Inward(i);
            Vector2 centre = run.At(i, at) + inward * KwRailing.Half;
            Vector2 side = inward * KwRailing.Post;
            Vector2[] corners = { centre - way - side, centre + way - side, centre + way + side, centre - way + side };
            Vector3[] ring = corners.Select(p => V(p, KwRailing.CapTop)).Concat(corners.Select(p => V(p, KwRailing.RailHeight))).ToArray();
            Solid.Hexa(b, ring);
        }

        // The outline's middle, which every edge's inside faces.
        private static Vector2 Middle(List<KwRoofSegment> edges)
        {
            Vector2 sum = Vector2.zero;
            foreach (KwRoofSegment edge in edges)
                sum += (edge.A + edge.B) / 2f;
            return edges.Count > 0 ? sum / edges.Count : Vector2.zero;
        }

        private static Vector3 V(Vector2 p, float z) => new Vector3(p.x, p.y, z);
    }
}
