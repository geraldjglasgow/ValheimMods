using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin as the steering gear climbs through it to a captain's wheel on a roof (<see cref="KwGearRooms"/>), in the
    /// rudder's kit frame (x forward from the post's after face, y left, z up from the rudder point): its room inside its
    /// bulkheads, drawn-in walls, raised sides and the hull's planking, from its floor to its roof's underside, less its
    /// corner posts (<see cref="KwCabinPosts"/>); whether its roof is over a place; its floor and roof's top; and its end
    /// nearer the stern, whose wall the shaft climbs: places in from that end and across the cabin (<see cref="Point"/>),
    /// the way to that end, and to each side. Plain maths over what the cabin saves, so every peer works out the same.
    /// </summary>
    internal sealed class KwGearRoom
    {
        // How near a roof's top a point must be to be on it; how far the side reaches are kept apart when kept.
        private const float OnTop = 0.06f, Grain = 0.02f;

        private readonly KwGearPose pose;
        private readonly KwCabinSpec spec;
        private readonly List<List<Vector3>> posts;
        private readonly Dictionary<(bool, int, int), float> reaches = new Dictionary<(bool, int, int), float>();
        private readonly float aft, fore, sign;

        public KwGearRoom(KwGearCabin cabin)
        {
            (Cabin, pose, spec) = (cabin, cabin.Pose, cabin.Spec);
            posts = KwCabinPosts.Of(spec);
            float back = spec.X0 + End(spec.Back), front = spec.X1 - End(spec.Front);
            sign = pose.Out(new Vector3(spec.X0, 0f, spec.Floor)).x <= pose.Out(new Vector3(spec.X1, 0f, spec.Floor)).x ? 1f : -1f;
            (aft, fore) = sign > 0f ? (back, front) : (front, back);
            Floor = pose.Out(new Vector3(spec.X0, 0f, spec.Floor)).z;
            Top = pose.Out(new Vector3(spec.X0, 0f, spec.Top)).z;
        }

        /// <summary>The cabin as the ship's pieces have it.</summary>
        public KwGearCabin Cabin { get; }

        /// <summary>The cabin's floor (the deck's or a roof's top under it) and its roof's top, as heights in the rudder's frame.</summary>
        public float Floor { get; }
        public float Top { get; }

        /// <summary>Its roof's underside.</summary>
        public float Under => Top - KwPlanks.Thick;

        /// <summary>How far the room runs in from its after end to its forward one.</summary>
        public float Length => Mathf.Abs(fore - aft);

        /// <summary>Whether its roof is over a place (kit x, y): inside its outline at its top.</summary>
        public bool Covers(float x, float y)
        {
            Vector3 k = pose.Local(new Vector3(x, y, Top));
            if (k.x < spec.X0 || k.x > spec.X1)
                return false;
            return k.y < KwCabinOutline.Reach(spec, spec.Left, k.x, spec.Top) && -k.y < KwCabinOutline.Reach(spec, spec.Right, k.x, spec.Top);
        }

        /// <summary>Whether a point stands on its roof's top.</summary>
        public bool OnRoof(Vector3 p) => Mathf.Abs(p.z - Top) < OnTop && Covers(p.x, p.y);

        /// <summary>
        /// Whether a point is inside the room at least a margin from everything round it: its ends' walls (a bulkhead, or the
        /// hull's end where the hull closes it), its sides (the hull's inside, a drawn-in wall's inner face), its roof's
        /// underside (not for a shaft going on up through it) and its corner posts. Its floor is not looked at: the shaft goes
        /// through it.
        /// </summary>
        public bool Free(Vector3 p, float margin, bool through = false)
        {
            Vector3 k = pose.Local(p);
            if ((!through && k.z > spec.Top - KwPlanks.Thick - margin) || k.x < Mathf.Min(aft, fore) + margin || k.x > Mathf.Max(aft, fore) - margin)
                return false;
            float z = Mathf.Clamp(k.z, spec.Floor + 0.05f, spec.Top - KwPlanks.Thick);
            if (k.y > Side(true, k.x, z) - margin || -k.y > Side(false, k.x, z) - margin)
                return false;
            return !InPost(k, margin);
        }

        /// <summary>A point of the rudder's frame at a height, in from the room's after end and across it (the cabin's own y, its left +).</summary>
        public Vector3 Point(float along, float across, float z)
        {
            Vector3 p = pose.Out(new Vector3(aft + sign * along, across, spec.Floor));
            return new Vector3(p.x, p.y, z);
        }

        /// <summary>Where a place of the rudder's frame (kit x, y) lies in the room's plan: in from its after end, and across (the cabin's own y).</summary>
        public Vector2 Plan(float x, float y)
        {
            Vector3 k = pose.Local(new Vector3(x, y, Top));
            return new Vector2(sign * (k.x - aft), k.y);
        }

        /// <summary>How far out a side's inside face is at a place in from the after end: the nearest it comes over the room's height.</summary>
        public float Reach(bool left, float along)
        {
            float x = aft + sign * along, least = float.MaxValue;
            for (float z = spec.Floor + 0.05f; z < spec.Top - KwPlanks.Thick; z += 0.4f)
                least = Mathf.Min(least, Side(left, x, z));
            return Mathf.Min(least, Side(left, x, spec.Top - KwPlanks.Thick));
        }

        /// <summary>The way (a unit, level) from inside the room to its after end, and to its left or right side.</summary>
        public Vector3 AftWay => Way(new Vector3(-sign, 0f, 0f));

        public Vector3 SideWay(bool left) => Way(new Vector3(0f, left ? 1f : -1f, 0f));

        // A way of the cabin's own frame in the rudder's frame, kept level.
        private Vector3 Way(Vector3 kit)
        {
            Vector3 w = pose.Out(kit) - pose.Out(Vector3.zero);
            return new Vector3(w.x, w.y, 0f).normalized;
        }

        // How thick an end is: the hull's planking where the hull closes it, else its bulkhead.
        private float End(KwCabinEnd end) => KwCabinOutline.Closed(spec, end) ? KwPlanks.Thick : KwCabinWall.Thick;

        // How far out a side's inside face is at a place and height (the cabin's own x, z), kept by the centimetre.
        private float Side(bool left, float x, float z)
        {
            (bool, int, int) key = (left, Mathf.RoundToInt(x / Grain), Mathf.RoundToInt(z / Grain));
            if (reaches.TryGetValue(key, out float known))
                return known;
            KwCabinSide side = left ? spec.Left : spec.Right;
            float reach = KwCabinOutline.Reach(spec, side, key.Item2 * Grain, key.Item3 * Grain) - (side.AtHull ? 0f : KwCabinWall.Thick);
            reaches[key] = reach;
            return reach;
        }

        // Whether a point of the cabin's frame is in a corner post (its square at the post's ring nearest that height) or near it.
        private bool InPost(Vector3 k, float margin)
        {
            float half = KwCabinPosts.Size / 2f + margin;
            foreach (List<Vector3> post in posts)
            {
                if (post.Count == 0 || k.z < post[0].z - margin || k.z > post[post.Count - 1].z + margin)
                    continue;
                Vector3 c = post[0];
                foreach (Vector3 ring in post)
                {
                    if (Mathf.Abs(ring.z - k.z) < Mathf.Abs(c.z - k.z))
                        c = ring;
                }
                if (Mathf.Abs(k.x - c.x) < half && Mathf.Abs(k.y - c.y) < half)
                    return true;
            }
            return false;
        }
    }
}
