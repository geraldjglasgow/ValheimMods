using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A railing on a cabin's roof as an anchor's cable sees it (<see cref="KwAnchorShip"/>), from its saved text as it is
    /// built (<see cref="KwRoofRail"/>, <see cref="KwRoofRailShape"/>), in its own kit frame on the roof's top: along each
    /// railed stretch of the roof's edge its cap and its top rail, each a rail's width in from the edge, and its posts
    /// between them; nothing where a ladder comes up. A windlass on that roof leads its cable over the cap between two
    /// posts to a chock seated on it (<see cref="KwAnchorRoofSpots"/>); any other cabin's roof is solid to over its railing.
    /// </summary>
    public sealed class KwAnchorRoofRail : KwAnchorSolid
    {
        private readonly KwAnchorPose pose;
        private readonly List<(Vector2 A, Vector2 B)> caps = new List<(Vector2 A, Vector2 B)>();
        private readonly List<(Vector2 At, Vector2 Way, Vector2 In)> posts = new List<(Vector2 At, Vector2 Way, Vector2 In)>();

        public KwAnchorRoofRail(KwAnchorPose pose, KwRoofRail rail) : base("a roof railing")
        {
            this.pose = pose;
            Vector2 middle = KwRoofRailShape.Middle(rail.Edges), min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
            foreach (List<Vector2> points in KwRoofRailShape.Runs(rail.Railed))
                Take(new KwRoofRun(points, middle, rail.Gaps), rail.Spacing);
            foreach (KwRoofSegment edge in rail.Edges)
                (min, max) = (Vector2.Min(min, Vector2.Min(edge.A, edge.B)), Vector2.Max(max, Vector2.Max(edge.A, edge.B)));
            Round(pose, new Vector3(min.x, min.y, 0f), new Vector3(max.x, max.y, KwRailing.RailHeight + KwRailing.TopRail));
        }

        // One run: each stretch of its cap's middle line (its gaps cut out) and each post, square to its edge, as built.
        private void Take(KwRoofRun run, float spacing)
        {
            for (int i = 0; i + 1 < run.Points.Count; i++)
            {
                Vector2 inward = run.Inward(i) * KwRailing.Half;
                foreach (Vector2 piece in run.Pieces(i))
                    caps.Add((run.At(i, piece.x) + inward, run.At(i, piece.y) + inward));
            }
            foreach (float at in KwRoofRailShape.Posts(run, spacing))
            {
                int i = run.Segment(at);
                posts.Add((run.At(i, at) + run.Inward(i) * KwRailing.Half, run.Way(i), run.Inward(i)));
            }
        }

        /// <summary>Whether its cap stands on the roof's edge at a point there (windlass kit): a railed stretch, no gap.</summary>
        public bool Covers(Vector3 edge)
        {
            Vector3 k = pose.Local(edge);
            Vector2 at = new Vector2(k.x, k.y);
            return Mathf.Abs(k.z) < 0.15f && caps.Exists(c => KwAnchorRod.Near(at, c.A, c.B) < KwRailing.Half + 0.03f);
        }

        /// <summary>Whether a point on the roof's edge (windlass kit) is within a gap of a post along the edge.</summary>
        public bool NearPost(Vector3 edge, float gap)
        {
            Vector3 k = pose.Local(edge);
            Vector2 at = new Vector2(k.x, k.y);
            return posts.Exists(q => Mathf.Abs(Vector2.Dot(at - q.At, q.Way)) < gap && Mathf.Abs(Vector2.Dot(at - q.At, q.In)) < 0.3f);
        }

        /// <summary>Where each post stands on the roof's edge (windlass kit).</summary>
        public List<Vector3> Posts() => posts.ConvertAll(q => pose.Out(new Vector3(q.At.x - q.In.x * KwRailing.Half, q.At.y - q.In.y * KwRailing.Half, 0f)));

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = pose.Local(p);
            Vector2 at = new Vector2(k.x, k.y);
            bool band = Band(k.z, 0f, KwRailing.CapTop, r) || Band(k.z, KwRailing.RailHeight, KwRailing.RailHeight + KwRailing.TopRail, r);
            if (band && caps.Exists(c => KwAnchorRod.Near(at, c.A, c.B) < KwRailing.Half + r))
                return true;
            return Band(k.z, KwRailing.CapTop, KwRailing.RailHeight, r)
                && posts.Exists(q => Mathf.Abs(Vector2.Dot(at - q.At, q.Way)) < KwRailing.Post + r && Mathf.Abs(Vector2.Dot(at - q.At, q.In)) < KwRailing.Post + r);
        }

        private static bool Band(float z, float low, float high, float r) => z > low - r && z < high + r;
    }
}
