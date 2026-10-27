using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's corner posts (the user's requests, 2026-10-25): at each corner where a bulkhead meets a side, a square post
    /// <see cref="Size"/> across, centred on the bulkhead's thickness so it shows on both its faces, from the deck (sunk into
    /// it as the walls are) up through the roof's edge to its top. Against a drawn-in wall it stands straight up, centred on
    /// the two walls' corner, so it covers both walls' ends. Against the hull its outboard face lies on the planking's
    /// inside at both its ends and at every height the hull's section turns, so it follows the hull's flare and narrowing
    /// with no gap; and where the hull's side is raised over the sheer, the post's outer part reaches on out over that
    /// side's end to just past its outside (its ear), so from the deck the corner is one post from the cabin's inside out to
    /// the ship's side. A cabin built on another's roof stands its posts on that roof's top, where the posts under them end:
    /// the same section at the same place, the grain by height (<see cref="KwCabinPostMesh"/>), so a post carried on up
    /// through both reads as one piece, and one ends at the lower roof where the upper cabin has no corner over it. The plank
    /// wood, as the railings (<see cref="KwRailLooks"/>); drawn only (nothing to bump into, so a doorway cut beside one is
    /// never narrowed). One wood a post (<see cref="KwHouseCost"/>). Kit axes.
    /// </summary>
    public static class KwCabinPosts
    {
        /// <summary>A post's square, in metres: along the cabin over the bulkhead, and in from the hull or round the corner.</summary>
        public const float Size = 0.24f;

        // How far a post's outboard face stands into the planking (no hairline at the hull); how far its ear reaches into
        // the raised side's end and stands proud of that side's outside (past its laps).
        private const float Seat = 0.005f, Into = 0.01f, Proud = 0.01f;

        /// <summary>One corner: the bulkhead's middle along the cabin, its end (-1 back, 1 front), its side and that side's sign (1 left, -1 right).</summary>
        private readonly struct Corner
        {
            public Corner(float x, float end, KwCabinSide side, float sign)
            {
                (X, End, Side, Sign) = (x, end, side, sign);
            }

            public float X { get; }
            public float End { get; }
            public KwCabinSide Side { get; }
            public float Sign { get; }
        }

        /// <summary>Every post's centre line, its points from the foot up to the roof's top.</summary>
        public static List<List<Vector3>> Of(KwCabinSpec spec) => Corners(spec).ConvertAll(c => Rings(spec, c).ConvertAll(Middle));

        /// <summary>How many posts a cabin has, for its cost.</summary>
        public static int Count(KwCabinSpec spec) => Corners(spec).Count;

        /// <summary>The posts and their ears (a front post's caps a hair lower than a back one's, an ear's lower than its post's).</summary>
        public static void Draw(MeshBuilder b, KwCabinSpec spec)
        {
            foreach (Corner c in Corners(spec))
            {
                KwCabinPostMesh.Draw(b, Rings(spec, c), c.X, Nudge(c.End));
                List<Vector3[]>? ear = Ear(spec, c);
                if (ear != null)
                    KwCabinPostMesh.Draw(b, ear, c.X, Nudge(c.End) + 0.002f);
            }
        }

        // Each bulkhead's two corners.
        private static List<Corner> Corners(KwCabinSpec spec)
        {
            List<Corner> corners = new List<Corner>();
            foreach (bool front in new[] { false, true })
            {
                if (spec.Unbuilt != null || KwCabinOutline.Closed(spec, front ? spec.Front : spec.Back))
                    continue;
                float x = front ? spec.X1 - KwCabinWall.Thick / 2f : spec.X0 + KwCabinWall.Thick / 2f, end = front ? 1f : -1f;
                corners.Add(new Corner(x, end, spec.Left, 1f));
                corners.Add(new Corner(x, end, spec.Right, -1f));
            }
            return corners;
        }

        /// <summary>
        /// Where a cabin's posts stand from: its deck, a little into it as its walls; on another cabin's roof (its sides
        /// measured lower down than its floor) the roof's top itself, where the posts of the cabin under it end.
        /// </summary>
        public static float Foot(KwCabinSpec spec) => spec.Floor > spec.Ref + 0.01f ? spec.Floor : spec.Floor - KwCabinOutline.Sink;

        // A post's rings from its foot to the roof's top: a square straight up at a drawn-in wall; against the hull its
        // outboard face on the planking's inside at its two ends, at every height the hull's section turns. Its outer end,
        // past the bulkhead, follows the hull there up to where the cabin's raised side starts, and over that the side's
        // end, where its ear stands (no hull there: the cabin's end is the hull carried up). Across a bulkhead too narrow
        // for both posts, each ends at the bulkhead's middle, so the two meet there.
        private static List<Vector3[]> Rings(KwCabinSpec spec, Corner c)
        {
            float foot = Foot(spec), wide = Wide(c.End);
            if (!c.Side.AtHull)
            {
                float y = KwCabinOutline.Reach(spec, c.Side, c.X, spec.Ref) - KwCabinWall.Thick / 2f, h = wide / 2f;
                return new List<float> { foot, spec.Top }.ConvertAll(z =>
                {
                    float near = Mathf.Max(y - h, Middle(spec, c, z));
                    return Ring(c.Sign, z, c.X - h, near, y + h, c.X + h, near, y + h);
                });
            }
            float outer = c.X + c.End * wide / 2f, inner = c.X - c.End * wide / 2f, end = End(spec, c), raised = Raised(spec, end);
            return Heights(spec, new[] { outer, end, inner }, foot, raised).ConvertAll(z =>
            {
                float wa = Inside(spec, z < raised - 0.001f ? outer : end, z), wb = Inside(spec, inner, z), middle = Middle(spec, c, z);
                return Ring(c.Sign, z, outer, Mathf.Max(wa - wide, middle), wa, inner, Mathf.Max(wb - wide, middle), wb);
            });
        }

        // The middle of a corner's bulkhead at a height, on that corner's side (out from the keel line on its sign).
        private static float Middle(KwCabinSpec spec, Corner c, float z) =>
            c.Sign * (KwCabinOutline.Reach(spec, spec.Left, c.X, z) - KwCabinOutline.Reach(spec, spec.Right, c.X, z)) / 2f;

        /// <summary>
        /// The ear over a raised side's end (none where the side is not raised): from that side's foot to the roof's top,
        /// from the post's outer end a little into the side's end, from the post's outboard face out past the side's laps.
        /// </summary>
        private static List<Vector3[]>? Ear(KwCabinSpec spec, Corner c)
        {
            float end = End(spec, c), foot = Raised(spec, end);
            if (!c.Side.AtHull || spec.Top - foot < 0.02f)
                return null;
            Vector2[] hull = KwCabinOutline.HullAt(spec, end);
            float outer = c.X + c.End * Wide(c.End) / 2f, into = end - c.End * Into, far = hull[hull.Length - 1].x + KwPlanks.Lap + Proud + Nudge(c.End);
            return Heights(spec, new[] { end }, foot, foot).ConvertAll(z =>
            {
                float w = Inside(spec, end, z);
                return Ring(c.Sign, z, outer, w, far, into, w, far);
            });
        }

        // The bulkhead's outer face along the cabin: its back end or its front.
        private static float End(KwCabinSpec spec, Corner c) => c.End > 0f ? spec.X1 : spec.X0;

        // Where the cabin's raised side starts at a place: on the sheer, or the posts' foot where that is higher (on a roof).
        private static float Raised(KwCabinSpec spec, float x)
        {
            Vector2[] hull = KwCabinOutline.HullAt(spec, x);
            return Mathf.Max(hull[hull.Length - 1].y, Foot(spec));
        }

        // The heights a post against the hull is ringed at: its foot, where the raised side starts, every point of the
        // planking's inside at these places between, and the roof's top.
        private static List<float> Heights(KwCabinSpec spec, float[] xs, float foot, float raised)
        {
            List<float> all = new List<float> { foot, spec.Top };
            if (raised > foot + 0.01f && raised < spec.Top - 0.01f)
                all.Add(raised);
            foreach (float x in xs)
            {
                foreach (Vector2 p in KwPlanks.Inside(KwCabinOutline.HullAt(spec, x)))
                {
                    if (p.y > foot + 0.01f && p.y < spec.Top - 0.01f)
                        all.Add(p.y);
                }
            }
            all.Sort();
            List<float> heights = new List<float>();
            foreach (float z in all)
            {
                if (heights.Count == 0 || z - heights[heights.Count - 1] > 0.01f)
                    heights.Add(z);
            }
            return heights;
        }

        // How far out the planking's inside is at x and height z (straight up from its top over the sheer, where the hull's
        // raised side stands no further in), the post's face a little into it. The hull's own section, nothing of the
        // cabin's height, so every cabin over the same keel line finds the same face.
        private static float Inside(KwCabinSpec spec, float x, float z) => KwShapes.WidthAt(KwPlanks.Inside(KwCabinOutline.HullAt(spec, x)), z) + Seat;

        // A front post a hair larger than a back one, so two cabins built end to end draw their posts' faces apart.
        private static float Nudge(float end) => end > 0f ? 0.002f : 0f;

        private static float Wide(float end) => Size + 2f * Nudge(end);

        // A ring at height z from two places along (xa, xb), each with how far in and out it reaches on the side's sign.
        private static Vector3[] Ring(float sign, float z, float xa, float inA, float outA, float xb, float inB, float outB) => new[]
        {
            new Vector3(xa, sign * outA, z), new Vector3(xb, sign * outB, z), new Vector3(xb, sign * inB, z), new Vector3(xa, sign * inA, z),
        };

        private static Vector3 Middle(Vector3[] ring) => (ring[0] + ring[1] + ring[2] + ring[3]) / 4f;
    }
}
