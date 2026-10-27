using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's own walls (<see cref="KwCabinWall"/>), one function per wall from the cabin's outline
    /// (<see cref="KwCabinOutline"/>): a bulkhead across each end the hull does not close, planked across from the deck to
    /// the roof's underside, its edges the hull's inside on a side at the hull and the drawn-in wall on the other; and a
    /// plank wall along each side drawn in from the hull, standing on the deck at its inset all along, following the
    /// hull's line, from the deck to the roof's underside. Every wall stands <see cref="KwCabinOutline.Sink"/> into the deck
    /// and none rises above the roof. Its doorways and windows are cut from each wall's outline (<see cref="KwCabinWall.Cut"/>,
    /// the cabin's openings on it). Kit axes.
    /// </summary>
    public static class KwCabinWalls
    {
        /// <summary>The walls' foot, a little into the deck, and their head, the roof's underside.</summary>
        public static float Foot(KwCabinSpec spec) => spec.Floor - KwCabinOutline.Sink;

        public static float Head(KwCabinSpec spec) => spec.Top - KwPlanks.Thick;

        /// <summary>The cabin's walls round it: its back bulkhead, its left wall, its front bulkhead, its right wall, those it has (none for a cabin that cannot be built).</summary>
        public static List<KwCabinWall> Of(KwCabinSpec spec)
        {
            List<KwCabinWall> walls = new List<KwCabinWall>();
            if (spec.Unbuilt != null)
                return walls;
            if (!KwCabinOutline.Closed(spec, spec.Back))
                walls.Add(Bulkhead(spec, false));
            if (!spec.Left.AtHull)
                walls.Add(Side(spec, true));
            if (!KwCabinOutline.Closed(spec, spec.Front))
                walls.Add(Bulkhead(spec, true));
            if (!spec.Right.AtHull)
                walls.Add(Side(spec, false));
            return walls;
        }

        /// <summary>The heights a wall's edge against the hull is measured at: from a height every quarter metre, and the roof's underside.</summary>
        public static List<float> Heights(KwCabinSpec spec, float from)
        {
            List<float> heights = new List<float>();
            float head = Head(spec);
            for (float z = from; z < head - 0.01f; z += 0.25f)
                heights.Add(z);
            heights.Add(head);
            return heights;
        }

        /// <summary>
        /// The bulkhead across the cabin at its back or its front: its line straight across from one side to the other (the
        /// back's from right to left, the front's from left to right, so the cabin's inside is to its right), its outline
        /// up the one edge and down the other.
        /// </summary>
        public static KwCabinWall Bulkhead(KwCabinSpec spec, bool front)
        {
            float x = front ? spec.X1 : spec.X0;
            List<float> heights = Heights(spec, Foot(spec));
            List<float> left = heights.ConvertAll(z => KwCabinOutline.Reach(spec, spec.Left, x, z));
            List<float> right = heights.ConvertAll(z => KwCabinOutline.Reach(spec, spec.Right, x, z));
            (float start, float way) = front ? (left.Max(), -1f) : (-right.Max(), 1f);
            float across = left.Max() + right.Max();
            List<Vector2> path = new List<Vector2> { new Vector2(x, start), new Vector2(x, start + way * across) };
            List<Vector2> outline = new List<Vector2>();
            List<float> first = front ? left : right.ConvertAll(r => -r), second = front ? right.ConvertAll(r => -r) : left;
            for (int i = 0; i < heights.Count; i++)
                outline.Add(new Vector2((first[i] - start) * way, heights[i]));
            for (int i = heights.Count - 1; i >= 0; i--)
                outline.Add(new Vector2((second[i] - start) * way, heights[i]));
            return new KwCabinWall(front ? "front" : "back", path, outline).Cut(spec.Openings);
        }

        /// <summary>
        /// The wall along a side drawn in from the hull: its line on the deck at the side's inset from the hull's inside, from
        /// the back to the front on the left (from the front back on the right, the cabin's inside to its right), and its
        /// outline from the foot to the roof's underside all along.
        /// </summary>
        public static KwCabinWall Side(KwCabinSpec spec, bool left)
        {
            KwCabinSide side = left ? spec.Left : spec.Right;
            List<float> xs = KwCabinOutline.Xs(spec);
            if (!left)
                xs.Reverse();
            List<Vector2> path = xs.ConvertAll(x => new Vector2(x, (left ? 1f : -1f) * KwCabinOutline.Reach(spec, side, x, spec.Ref)));
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector2.Distance(path[i - 1], path[i]);
            float foot = Foot(spec), head = Head(spec);
            List<Vector2> outline = new List<Vector2> { new Vector2(0f, foot), new Vector2(length, foot), new Vector2(length, head), new Vector2(0f, head) };
            return new KwCabinWall(left ? "left" : "right", path, outline).Cut(spec.Openings);
        }

        /// <summary>An outline's area in m² (both faces of the wall count once), for the cabin's cost.</summary>
        public static float Area(List<Vector2> outline)
        {
            float twice = 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 a = outline[i], c = outline[(i + 1) % outline.Count];
                twice += a.x * c.y - c.x * a.y;
            }
            return Mathf.Abs(twice) / 2f;
        }
    }
}
