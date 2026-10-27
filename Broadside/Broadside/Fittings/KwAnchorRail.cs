using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hull railing as an anchor's cable sees it (<see cref="KwAnchorShip"/>), from its saved span as it is built
    /// (<see cref="KwRailing"/>): on each side it stands on, its cap along the sheer, its top rail and its posts, and the
    /// places of its shield hooks (<see cref="KwShieldSlots"/>, numbered as they are) and which have a shield hung, in its own
    /// kit frame. A fairlead on its cap stands between two posts (snug beside one at the nearest), so the cable runs between
    /// the posts under the top rail, and away from a hung shield where it can.
    /// </summary>
    public sealed class KwAnchorRail : KwAnchorSolid
    {
        private readonly KwAnchorPose pose;
        private readonly List<Station> stations;
        private readonly List<float> sides, posts;
        private readonly List<(float X, float Side, bool Hung)> hooks = new List<(float X, float Side, bool Hung)>();

        /// <summary>A railing from its span, with the numbers of its hooks that have a shield hung (its saved shields).</summary>
        public KwAnchorRail(KwAnchorPose pose, KwSpan span, ICollection<int> hung) : base("the railing")
        {
            (this.pose, stations) = (pose, span.Stations());
            sides = KwRailing.Drawn(KwRailText.Sides(span.Rail));
            posts = KwRailing.PostsAlong(stations, KwRailText.Spacing(span.Rail));
            Hooks(hung);
            float wide = KwAnchorHull.Widest(stations) + KwRailing.Half;
            float low = stations.Min(st => st.Half[st.Half.Length - 1].y), high = KwAnchorHull.Highest(stations) + KwRailing.RailHeight + KwRailing.TopRail;
            Round(pose, new Vector3(From, -wide, low - 0.1f), new Vector3(To, wide, high));
        }

        // The shield hooks' places along each side, as KwShieldSlots spaces and numbers them: the length shared out about
        // every Spacing, each hook in the middle of its share, none where the hull is narrower than Narrowest.
        private void Hooks(ICollection<int> hung)
        {
            float length = To - From;
            int count = length < 0.6f ? 0 : Mathf.Max(1, Mathf.RoundToInt(length / KwShieldSlots.Spacing));
            foreach (float side in sides)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = From + (i + 0.5f) * length / count;
                    if (KwRailing.EdgeAt(stations, x).x >= KwShieldSlots.Narrowest)
                        hooks.Add((x, side, hung.Contains(hooks.Count)));
                }
            }
        }

        private float From => stations[0].X;
        private float To => stations[stations.Count - 1].X;

        /// <summary>
        /// Whether it stands on the sheer at a point (windlass kit, on a planking part's sheer): the point's place along it
        /// and the side (+1 its left) it is on; false where it does not reach.
        /// </summary>
        public bool Covers(Vector3 sheer, out float x, out float side)
        {
            Vector3 k = pose.Local(sheer);
            (x, side) = (k.x, Mathf.Sign(k.y));
            if (k.x < From - 0.01f || k.x > To + 0.01f || !sides.Contains(side))
                return false;
            Vector2 e = KwRailing.EdgeAt(stations, k.x);
            return Mathf.Abs(e.x - Mathf.Abs(k.y)) < 0.12f && Mathf.Abs(e.y - k.z) < 0.15f;
        }

        /// <summary>Whether a place along it is within a gap of a post (on any side).</summary>
        public bool Near(float x, float gap)
        {
            foreach (float post in posts)
            {
                if (Mathf.Abs(post - x) < gap)
                    return true;
            }
            return false;
        }

        /// <summary>What a place along it on a side counts for within a gap of a shield hook there: more for a hook with a shield hung.</summary>
        public float Hooked(float x, float side, float gap, float empty, float hung)
        {
            float cost = 0f;
            foreach ((float at, float s, bool shield) in hooks)
            {
                if (s == side && Mathf.Abs(at - x) < gap)
                    cost = Mathf.Max(cost, shield ? hung : empty);
            }
            return cost;
        }

        /// <summary>Where each of its posts stands on the sheer, on each side it stands on (windlass kit).</summary>
        public List<Vector3> Posts()
        {
            List<Vector3> at = new List<Vector3>();
            foreach (float side in sides)
            {
                foreach (float x in posts)
                {
                    Vector2 e = KwRailing.EdgeAt(stations, x);
                    at.Add(pose.Out(new Vector3(x, side * e.x, e.y)));
                }
            }
            return at;
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = pose.Local(p);
            if (k.x < From - r || k.x > To + r)
                return false;
            Vector2 e = KwRailing.EdgeAt(stations, Mathf.Clamp(k.x, From, To));
            foreach (float side in sides)
            {
                float off = Mathf.Abs(k.y - side * e.x), up = k.z - e.y;
                if (off < KwRailing.Half + r && (Band(up, 0f, KwRailing.CapTop, r) || Band(up, KwRailing.RailHeight, KwRailing.RailHeight + KwRailing.TopRail, r)))
                    return true;
                if (Post(k, side, r))
                    return true;
            }
            return false;
        }

        private static bool Band(float z, float low, float high, float r) => z > low - r && z < high + r;

        // A post on one side: its square from the cap up to the top rail.
        private bool Post(Vector3 k, float side, float r)
        {
            foreach (float x in posts)
            {
                if (Mathf.Abs(k.x - Mathf.Clamp(x, From + KwRailing.Post, To - KwRailing.Post)) >= KwRailing.Post + r)
                    continue;
                Vector2 e = KwRailing.EdgeAt(stations, x);
                if (Mathf.Abs(k.y - side * e.x) < KwRailing.Post + r && Band(k.z - e.y, KwRailing.CapTop, KwRailing.RailHeight, r))
                    return true;
            }
            return false;
        }
    }
}
