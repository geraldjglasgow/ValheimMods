using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One place on a railing's outside where a shield hangs: where (the railing part's own frame, Unity axes) and turned so its +z points out of the hull, level.</summary>
    public readonly struct KwShieldSlot
    {
        public KwShieldSlot(Vector3 at, Quaternion turn)
        {
            At = at;
            Turn = turn;
        }

        public Vector3 At { get; }
        public Quaternion Turn { get; }
    }

    /// <summary>
    /// Where a railing (<see cref="KwRailing"/>) has its shield hooks: along each side's outside, about every
    /// <see cref="Spacing"/> metres (as many as come nearest, the part's length shared out evenly among them, so 0.9 to
    /// 1.5 m apart, two on a 2 m part; one at least on a part 0.6 m long or more, each
    /// in the middle of its share), <see cref="Out"/> outboard of the rail's line and <see cref="Up"/> over its foot, so a
    /// shield hangs over the rail's posts; never where the hull is narrower than <see cref="Narrowest"/> each side of its
    /// middle (towards a stem, where the two sides' shields would meet); a one-sided railing has the hooks of its own side
    /// only. Port side first, then starboard, aft to fore: the
    /// order numbers the hooks, the same on every peer, on the dock and on a launched ship. Worked out from the railing's
    /// saved span alone (<see cref="KwSpan"/>, as built from its clicks), so every peer and the ship agree.
    /// </summary>
    public static class KwShieldSlots
    {
        // Out: the game's wall item stand's plate is 0.1 m thick about its origin, so its front stands 0.05 out of the
        // origin; here that front lies on the rail's outside (the beams' 0.06 off its line) with 1 cm to spare.
        public const float Spacing = 1.2f, Out = 0.02f, Up = 0.4f, Narrowest = 0.4f;

        /// <summary>The hooks of a railing from its saved text; none when it does not read.</summary>
        public static List<KwShieldSlot> Of(string saved)
        {
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return new List<KwShieldSlot>();
            KwDef rail = span.Def("rail");
            return Along(rail.Stations, new Vector3(rail.Pivot.x, 0f, rail.Pivot.y), KwRailText.Sides(span.Rail));
        }

        /// <summary>The hooks along a railing's stations (kit axes), placed around its pivot, on the sides it stands on (<see cref="KwRailText"/>).</summary>
        public static List<KwShieldSlot> Along(List<Station> st, Vector3 pivot, string sides = KwRailText.Both) =>
            Spots(st, pivot, sides).ConvertAll(s => new KwShieldSlot(s.At, Quaternion.LookRotation(s.Facing, Vector3.up)));

        /// <summary>Where a railing's hooks are, from its saved text (as <see cref="Of"/> places them), without their turns: plain arithmetic.</summary>
        public static List<Vector3> Places(string saved)
        {
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return new List<Vector3>();
            KwDef rail = span.Def("rail");
            return Spots(rail.Stations, new Vector3(rail.Pivot.x, 0f, rail.Pivot.y), KwRailText.Sides(span.Rail)).ConvertAll(s => s.At);
        }

        // Each hook's place and the way out of the hull there, side by side, evenly along.
        private static List<(Vector3 At, Vector3 Facing)> Spots(List<Station> st, Vector3 pivot, string sides)
        {
            List<(Vector3 At, Vector3 Facing)> spots = new List<(Vector3, Vector3)>();
            if (st.Count < 2)
                return spots;
            float x0 = st[0].X, length = st[st.Count - 1].X - x0;
            int count = length < 0.6f ? 0 : Mathf.Max(1, Mathf.RoundToInt(length / Spacing));
            foreach (float side in KwRailing.Drawn(sides))
            {
                for (int i = 0; i < count; i++)
                {
                    if (Place(st, x0 + (i + 0.5f) * length / count, side, pivot) is (Vector3 at, Vector3 facing))
                        spots.Add((at, facing));
                }
            }
            return spots;
        }

        /// <summary>
        /// The hook at x along one side (Unity axes, around the pivot) and the way out of the hull there: the rail's edge
        /// (between the stations either side, as its beams run), moved out square to the rail and up; none where the hull
        /// is too narrow.
        /// </summary>
        internal static (Vector3 At, Vector3 Facing)? Place(List<Station> st, float x, float side, Vector3 pivot)
        {
            int s = 0;
            while (s + 2 < st.Count && x > st[s + 1].X)
                s++;
            Vector2 a = Edge(st[s]), c = Edge(st[s + 1]);
            float run = st[s + 1].X - st[s].X, f = run < 1e-4f ? 0f : Mathf.Clamp01((x - st[s].X) / run);
            Vector2 e = Vector2.Lerp(a, c, f);
            if (e.x < Narrowest)
                return null;
            Vector2 way = new Vector2(run, side * (c.x - a.x)), outward = side * new Vector2(-way.y, way.x).normalized;
            Vector3 kit = new Vector3(x, side * e.x, e.y + Up) + new Vector3(outward.x, outward.y, 0f) * Out;
            return (KwMesh.ToUnity(kit - pivot), KwMesh.ToUnity(new Vector3(outward.x, outward.y, 0f)));
        }

        // A station's top edge, where the railing stands (half width, height).
        private static Vector2 Edge(Station s) => s.Half[s.Half.Length - 1];
    }
}
