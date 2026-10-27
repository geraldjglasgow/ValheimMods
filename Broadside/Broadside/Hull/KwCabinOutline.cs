using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin measured from what it saves (<see cref="KwCabinSpec"/>), in its kit axes (x from its back end along its
    /// frame, y across, its left +, z up from the keel line): the hull's section anywhere along it, carried straight up
    /// from the sheer to the roof where the hull is lower; how far the cabin reaches on each side at any place and height
    /// (the hull's inside on a side at the hull; a drawn-in side's wall, standing on the deck that far in from the hull's
    /// inside at <see cref="KwCabinSpec.Ref"/>, upright); how wide it is across anywhere (<see cref="Across"/>, what
    /// <see cref="KwCabinFit"/> fits it by); which ends the hull closes (the deck sections reach its end there, nothing
    /// drawn in); the roof's outline; and why no cabin fits at all. Pure numbers.
    /// </summary>
    public static class KwCabinOutline
    {
        /// <summary>
        /// Metres: how far the walls stand into the deck, the shortest cabin (two corner posts' room), and the narrowest it
        /// may be anywhere, a bulkhead at its deck too (a wall, with no room for a doorway; its posts meet). Before the
        /// user's report of 2026-10-25 they were 1 m long and 0.6 m across a bulkhead, with the hull's sides raised at most
        /// 3 m, and a cabin past any of them was refused outright.
        /// </summary>
        public const float Sink = 0.05f, MinLength = 0.5f, MinWide = 0.3f;

        /// <summary>The hull's section at x along the cabin: blended between the sections either side of it (when they have as many points), else the nearer.</summary>
        public static Vector2[] HullAt(KwCabinSpec spec, float x)
        {
            List<Station> st = spec.Stations;
            int after = st.FindIndex(s => s.X >= x);
            if (after <= 0)
                return st[after == 0 ? 0 : st.Count - 1].Half;
            Station a = st[after - 1], c = st[after];
            float span = c.X - a.X;
            if (span < 1e-4f || a.Half.Length != c.Half.Length)
                return x - a.X < c.X - x ? a.Half : c.Half;
            return KwJoints.Blend(a.Half, c.Half, (x - a.X) / span);
        }

        /// <summary>The hull's section at x carried straight up from its sheer to the roof's top.</summary>
        public static Station Up(KwCabinSpec spec, float x) => Extend(new Station(x, HullAt(spec, x)), spec.Top);

        /// <summary>A section carried straight up from its sheer to a height, when it is lower.</summary>
        public static Station Extend(Station s, float top)
        {
            Vector2 sheer = s.Half[s.Half.Length - 1];
            if (top <= sheer.y + 0.001f)
                return s;
            Vector2[] half = new Vector2[s.Half.Length + 1];
            s.Half.CopyTo(half, 0);
            half[half.Length - 1] = new Vector2(sheer.x, top);
            return new Station(s.X, half);
        }

        /// <summary>Whether the hull closes an end: the deck sections reach the hull's end there, it is not drawn in, both sides stand at the hull.</summary>
        public static bool Closed(KwCabinSpec spec, KwCabinEnd end) =>
            end.Hull != KwCabinEnd.Open && end.Inset < 0.001f && spec.Left.AtHull && spec.Right.AtHull;

        /// <summary>How far out the cabin reaches on one side at x and height z: the hull's inside there, or its drawn-in wall's outer face.</summary>
        public static float Reach(KwCabinSpec spec, KwCabinSide side, float x, float z) => Reach(spec, side, Up(spec, x).Half, z);

        private static float Reach(KwCabinSpec spec, KwCabinSide side, Vector2[] half, float z) =>
            side.AtHull ? KwShapes.InsideWidth(half, z) : KwShapes.InsideWidth(half, spec.Ref) - side.Total;

        /// <summary>How wide the cabin is across at x, both sides' reach together: the narrower of just over its deck and its roof's top.</summary>
        public static float Across(KwCabinSpec spec, float x)
        {
            Vector2[] half = Up(spec, x).Half;
            float deck = Reach(spec, spec.Left, half, spec.Floor + 0.05f) + Reach(spec, spec.Right, half, spec.Floor + 0.05f);
            return Mathf.Min(deck, Reach(spec, spec.Left, half, spec.Top) + Reach(spec, spec.Right, half, spec.Top));
        }

        /// <summary>The places along the cabin its walls and roof are drawn at: its two ends and every hull section between.</summary>
        public static List<float> Xs(KwCabinSpec spec)
        {
            List<float> xs = new List<float> { spec.X0 };
            foreach (Station s in spec.Stations)
            {
                if (s.X > spec.X0 + 0.01f && s.X < spec.X1 - 0.01f)
                    xs.Add(s.X);
            }
            xs.Add(spec.X1);
            return xs;
        }

        /// <summary>The hull's sections along the cabin's own length, cut at its ends.</summary>
        public static List<Station> Inside(KwCabinSpec spec) => Xs(spec).ConvertAll(x => new Station(x, HullAt(spec, x)));

        /// <summary>
        /// The roof's outline at its top, along the cabin: how far it reaches left and right at each place. Both sides at
        /// the hull: the hull's deck edge at the roof's height, ending where its width runs out at a pointed end the hull closes.
        /// </summary>
        public static List<(float X, float Left, float Right)> Roof(KwCabinSpec spec)
        {
            if (spec.Unbuilt != null)
                return new List<(float X, float Left, float Right)>();
            if (spec.Left.AtHull && spec.Right.AtHull)
            {
                List<Station> up = Inside(spec).ConvertAll(s => Extend(s, spec.Top));
                return KwShapes.DeckEdge(up, _ => spec.Top, KwShapes.InsideWidth).ConvertAll(e => (e.x, e.y, e.y));
            }
            return Xs(spec).ConvertAll(x => (x, Reach(spec, spec.Left, x, spec.Top), Reach(spec, spec.Right, x, spec.Top)));
        }

        /// <summary>How far the hull's sides are raised at most, from the sheer (or the deck, where it is over the sheer) to the roof's top.</summary>
        public static float Raise(KwCabinSpec spec)
        {
            float most = 0f;
            if (!spec.Left.AtHull && !spec.Right.AtHull)
                return most;
            foreach (Station s in Inside(spec))
                most = Mathf.Max(most, spec.Top - Mathf.Max(s.Half[s.Half.Length - 1].y, spec.Floor));
            return most;
        }

        /// <summary>The hull's sheer at its highest along the cabin.</summary>
        public static float Sheer(KwCabinSpec spec)
        {
            float most = float.MinValue;
            foreach (Station s in Inside(spec))
                most = Mathf.Max(most, s.Half[s.Half.Length - 1].y);
            return most;
        }

        /// <summary>
        /// Why no cabin can be built here at all, null when one can: a spec read or made is already fitted to its hull
        /// (<see cref="KwCabinFit"/>: heights and insets limited, ends drawn in where the hull narrows), so only deck
        /// sections too short or a hull too narrow all along them even with no insets are left.
        /// </summary>
        public static string? Refused(KwCabinSpec spec) => spec.Unbuilt;
    }
}
