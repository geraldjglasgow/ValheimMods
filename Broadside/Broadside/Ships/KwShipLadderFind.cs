using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a launched ship's boarding ladder goes (<see cref="KwShipLadderPlan"/>), worked out from its blueprint alone
    /// (<see cref="KwShipLadderShip"/>), the same on every peer. Best first, nearest the hull's widest section, the port
    /// side before the starboard (a metre along counts the same as the other side):
    /// <list type="number">
    /// <item>a stretch of the hull's side with no cabin standing at the hull there (the sheer itself is its top), clear of
    /// openings, of a railing's shield hooks and of its posts under the hooks, of where the anchor's cable leaves the ship,
    /// of a rudder and of the fittings outside the hull, with a deck (else a hold floor) inside the rail at that height and
    /// room to stand there, inside no cabin;</item>
    /// <item>else a doorway cut in the hull's side or a cabin's raised side whose floor is there to stand on: the ladder up
    /// below it to its sill, the climb to inside it (into that cabin's room);</item>
    /// <item>else the lowest cabin roof's edge over a side at the hull with room to stand on the roof: the ladder up the
    /// raised side, the climb onto the roof;</item>
    /// <item>else, as before, the widest section's sheer on the port side, the climb to a floor inside it or the nearest
    /// place to stand aboard.</item>
    /// </list>
    /// Never a climb into a closed cabin or into the air. The ladder reaches from <see cref="UnderWater"/> under the
    /// waterline to its top, over a railing's cap where it hooks over one.
    /// </summary>
    public static class KwShipLadderFind
    {
        /// <summary>How far under the waterline the ladder's foot reaches; the shortest climb over it.</summary>
        public const float UnderWater = 0.35f, MinClimb = 0.4f;

        // What the starboard side costs as metres along; how far over the floor the climb puts the player; how far over a
        // railing's cap the ladder's top stands.
        private const float Starboard = 1f, Lift = 0.12f, OverCap = KwRailing.CapTop - 0.04f;

        // How steeply the hull's side may run in or out along the ship (metres across a metre along) and still count as a
        // straight side (the runs between ribs); a place on a curve (a stem's, a stern's) ranks after every straight one.
        private const float Straight = 0.06f, Curved = 100f, Probe = 0.4f;
        private static readonly float[] Insets = { 0.6f, 0.45f, 0.8f, 1f }, Shifts = { 0f, 0.3f, -0.3f };

        public static KwShipLadderPlan For(KwBlueprint blueprint, KwBpHull hull)
        {
            KwShipLadderShip ship = new KwShipLadderShip(blueprint, hull);
            float foot = Mathf.Max(hull.Keel + 0.05f, ship.Water - UnderWater);
            return OnDeck(ship, foot) ?? Doorway(ship, foot) ?? OnRoof(ship, foot) ?? Widest(ship, hull, foot);
        }

        // 1. The hull's own sheer, uncovered, with a deck or hold floor inside the rail.
        private static KwShipLadderPlan? OnDeck(KwShipLadderShip ship, float foot)
        {
            List<KwRigEdge> edges = Ranked(ship, ship.Rig.Edges.FindAll(e => e.Roof == null), false);
            foreach (KwRigEdge edge in edges)
            {
                Vector3 at = edge.At;
                if (at.z - foot < MinClimb || !Run(ship, edge) || KwShipLadderRoom.Railing(ship, at) != null)
                    continue;
                KwShipLadderPlan? plan = Plan(ship, KwShipLadderPlan.OnDeck, at, foot, at.z + (edge.Railed ? OverCap : 0f), null);
                if (plan != null)
                    return plan;
            }
            return null;
        }

        // 2. A doorway in a side, the ladder up to its sill, the climb inside it.
        private static KwShipLadderPlan? Doorway(KwShipLadderShip ship, float foot)
        {
            List<KwLadderHole> doors = ship.Holes.FindAll(h => h.Door);
            doors.Sort((a, b) => Better(Rank(ship, new Vector3(a.Middle, a.Side, a.Low)), Rank(ship, new Vector3(b.Middle, b.Side, b.Low)),
                new Vector3(a.Middle, a.Side, a.Low), new Vector3(b.Middle, b.Side, b.Low)));
            foreach (KwLadderHole door in doors)
            {
                float x = door.Middle, side = door.Side;
                Vector3 face = new Vector3(x, side * Outside(ship, x, side, door.Low + 0.3f), door.Low);
                if (door.Low - foot < MinClimb || face.y == 0f)
                    continue;
                KwShipLadderPlan? plan = Plan(ship, KwShipLadderPlan.AtDoorway, face, foot, door.Low, c => Opens(c, x, side));
                if (plan != null)
                    return plan;
            }
            return null;
        }

        // 3. The lowest roof's edge over a side at the hull, the climb onto the roof.
        private static KwShipLadderPlan? OnRoof(KwShipLadderShip ship, float foot)
        {
            List<KwRigEdge> edges = Ranked(ship, ship.Rig.Edges.FindAll(e => e.Roof != null && AtHull(e.Roof, e.At)), true);
            foreach (KwRigEdge edge in edges)
            {
                Vector3 at = edge.At;
                if (at.z - foot < MinClimb || !Run(ship, edge))
                    continue;
                KwShipLadderPlan? plan = Plan(ship, KwShipLadderPlan.OnRoof, at, foot, at.z + (edge.Railed ? OverCap : 0f), null);
                if (plan != null)
                    return plan;
            }
            return null;
        }

        // A ladder at the edge at, up to top: clear of what is in the way, its climb to a floor near the edge's height.
        private static KwShipLadderPlan? Plan(KwShipLadderShip ship, string kind, Vector3 at, float foot, float top, System.Predicate<KwRigCabin>? into)
        {
            float side = Mathf.Sign(at.y);
            List<Vector2> profile = Profile(ship, at.x, side, foot, top, out float widest);
            if (KwShipLadderRoom.Blocked(ship, at.x, side, foot, top, widest + KwShipLadderPlan.Clear) != null)
                return null;
            Vector3? target = Inboard(ship, at, kind, into);
            float slope = Slope(ship, at.x, side, foot, top);
            if (Mathf.Abs(slope) > Straight)
                widest = Middle(profile);                         // turned to the side's curve: its rails meet the hull alike
            return target == null ? null : new KwShipLadderPlan(kind, at.x, side, foot, top, ship.Water, profile, widest, target.Value, slope);
        }

        // 4. Nothing found: the widest section's sheer on the port side, the climb to a floor inside it if there is one.
        private static KwShipLadderPlan Widest(KwShipLadderShip ship, KwBpHull hull, float foot)
        {
            float x = hull.SectionZ, top = hull.Keel + hull.Section[hull.Section.Length - 1].y;
            Vector3 at = new Vector3(x, Mathf.Max(0.3f, Outside(ship, x, 1f, top)), top);
            Vector3 target = Inboard(ship, at, KwShipLadderPlan.Widest, null) ?? Anywhere(ship, hull) ?? new Vector3(x, Mathf.Max(0f, at.y - 0.6f), hull.Keel + hull.DeckTop + 0.1f);
            return new KwShipLadderPlan(KwShipLadderPlan.Widest, x, 1f, foot, top, ship.Water, Profile(ship, x, 1f, foot, top, out float widest), widest, target);
        }

        // The place to stand nearest the widest section anywhere aboard (every 0.5 m along and across): on a deck, a hold
        // floor or a roof, inside no cabin; null on a ship with nothing to stand on.
        private static Vector3? Anywhere(KwShipLadderShip ship, KwBpHull hull)
        {
            for (float off = 0f; off <= hull.Length; off += 0.5f)
            {
                foreach (float x in new[] { ship.Widest - off, ship.Widest + off })
                {
                    for (int i = 0; i <= hull.Beam / 0.5f; i++)
                    {
                        // across from the middle line out, port and starboard in turn: 0, 0.5, -0.5, 1, -1 ...
                        Vector3 p = new Vector3(x, (i + 1) / 2 * 0.5f * (i % 2 == 1 ? 1f : -1f), 0f);
                        if (KwShipLadderRoom.Stand(ship, p, float.MaxValue, null, true, true) is float floor)
                            return new Vector3(p.x, p.y, floor + Lift);
                    }
                }
            }
            return null;
        }

        // The climb's place inside an edge (insets in from it, shifts along), on what that kind of ladder climbs to: a deck
        // up to a little over the sheer (else a hold floor); a floor at a doorway's sill; the roof whose edge it is. Of the
        // places, the one on the highest floor (the deck at the rail's height before one under a step), the nearer first.
        private static Vector3? Inboard(KwShipLadderShip ship, Vector3 at, string kind, System.Predicate<KwRigCabin>? into)
        {
            bool roof = kind == KwShipLadderPlan.OnRoof, door = kind == KwShipLadderPlan.AtDoorway;
            float limit = at.z + (roof ? 0.1f : door ? 0.4f : 0.25f), lowest = roof ? at.z - 0.12f : door ? at.z - 0.45f : float.MinValue;
            foreach (bool holds in new[] { false, true })
            {
                Vector3? best = null;
                foreach (Vector3 p in Spots(at))
                {
                    if (KwShipLadderRoom.Stand(ship, p, limit, into, holds && !roof, roof) is float floor && floor >= lowest && (best == null || floor + Lift > best.Value.z + 0.05f))
                        best = new Vector3(p.x, p.y, floor + Lift);
                }
                if (best != null)
                    return best;
            }
            return null;
        }

        // The places tried inside an edge: in from it by each inset, along it by each shift, never past the middle line.
        private static IEnumerable<Vector3> Spots(Vector3 at)
        {
            float side = Mathf.Sign(at.y), across = Mathf.Abs(at.y);
            foreach (float inset in Insets)
            {
                foreach (float shift in Shifts)
                {
                    if (across - inset > 0.05f)
                        yield return new Vector3(at.x + shift, side * (across - inset), 0f);
                }
            }
        }

        // Whether the ladder's whole width hangs from this edge: its neighbours of the same edge along it every step, level.
        private static bool Run(KwShipLadderShip ship, KwRigEdge edge)
        {
            int found = 0;
            foreach (KwRigEdge e in ship.Rig.Edges)
            {
                if (e.Roof == edge.Roof && Mathf.Sign(e.At.y) == Mathf.Sign(edge.At.y) && Mathf.Abs(e.At.x - edge.At.x) <= KwShipLadderRoom.Half + 0.01f && Mathf.Abs(e.At.z - edge.At.z) < 0.15f)
                    found++;
            }
            return found >= Mathf.RoundToInt(2f * KwShipLadderRoom.Half / KwRigEdges.Step);
        }

        // How far from the best place a place is: metres along from the widest section, the starboard side a metre more, and
        // any place where the side curves (a stem's or a stern's) after every straight one (the user, 2026-10-26: "I'd prefer
        // to place ladders on any of the pieces that are between supports").
        private static float Rank(KwShipLadderShip ship, Vector3 at) =>
            Mathf.Abs(at.x - ship.Widest) + (at.y < 0f ? Starboard : 0f) + (Mathf.Abs(Slope(ship, at.x, Mathf.Sign(at.y), at.z - 1.5f, at.z)) > Straight ? Curved : 0f);

        /// <summary>
        /// How fast the hull's side runs out (+) or in (-) along the ship at a place, metres across a metre forward, the mean
        /// over the heights a ladder there spans where the hull is; 0 where it is straight or nothing is there.
        /// </summary>
        public static float Slope(KwShipLadderShip ship, float x, float side, float foot, float top)
        {
            float sum = 0f;
            int n = 0;
            for (int i = 0; i <= 4; i++)
            {
                float z = Mathf.Lerp(foot, top - 0.1f, i / 4f), aft = Outside(ship, x - Probe, side, z), fore = Outside(ship, x + Probe, side, z);
                if (aft <= 0f || fore <= 0f)
                    continue;
                (sum, n) = (sum + (fore - aft) / (2f * Probe), n + 1);
            }
            return n > 0 ? sum / n : 0f;
        }

        // The hull's widest under the ladder's middle (a turned ladder's line follows the side, so its width needs no more).
        private static float Middle(List<Vector2> profile)
        {
            float most = 0f;
            foreach (Vector2 p in profile)
                most = Mathf.Max(most, p.y);
            return most;
        }

        // The better of two places (kit x along, y its side) by their ranks: the nearer, then aft first, then port first, so
        // every peer takes the same of two as good.
        private static int Better(float ra, float rb, Vector3 a, Vector3 b)
        {
            int rank = ra.CompareTo(rb);
            return rank != 0 ? rank : a.x != b.x ? a.x.CompareTo(b.x) : b.y.CompareTo(a.y);
        }

        // Edges best first, each ranked once; roofs: the lower roof first (to a decimetre), then the better place.
        private static List<KwRigEdge> Ranked(KwShipLadderShip ship, List<KwRigEdge> edges, bool roofs)
        {
            Dictionary<KwRigEdge, float> rank = new Dictionary<KwRigEdge, float>();
            foreach (KwRigEdge e in edges)
                rank[e] = Rank(ship, e.At);
            edges.Sort((a, b) =>
            {
                int high = roofs ? Mathf.RoundToInt(a.At.z * 10f).CompareTo(Mathf.RoundToInt(b.At.z * 10f)) : 0;
                return high != 0 ? high : Better(rank[a], rank[b], a.At, b.At);
            });
            return edges;
        }

        // Whether a roof's edge is over its cabin's side at the hull (a drawn-in side stands on the deck, reached from it).
        private static bool AtHull(KwRigCabin cabin, Vector3 at) => (cabin.Pose.Local(at).y >= 0f ? cabin.Spec.Left : cabin.Spec.Right).AtHull;

        // Whether a cabin's side at the hull on a side of the ship stands at a place along: a doorway there opens into it.
        private static bool Opens(KwRigCabin cabin, float x, float side)
        {
            Vector3 k = cabin.Pose.Local(new Vector3(x, side * 0.5f, cabin.Spec.Floor)), middle = cabin.Pose.Local(new Vector3(x, 0f, cabin.Spec.Floor));
            KwCabinSide wall = k.y >= middle.y ? cabin.Spec.Left : cabin.Spec.Right;
            return wall.AtHull && k.x >= cabin.Spec.X0 - 0.05f && k.x <= cabin.Spec.X1 + 0.05f;
        }

        /// <summary>The hull's outside half width at a height and place along on a side, its planking's or a cabin's raised side's (0: none there).</summary>
        public static float Outside(KwShipLadderShip ship, float x, float side, float z)
        {
            float most = 0f;
            foreach (KwAnchorHull hull in ship.Rig.Hull.Hulls)
                most = Mathf.Max(most, KwShipLadderSides.Planking(hull, x, side, z));
            foreach (KwRigCabin cabin in ship.Rig.Cabins)
                most = Mathf.Max(most, KwShipLadderSides.Raised(cabin, x, side, z));
            return most;
        }

        // The hull's outside under the ladder's middle every 0.1 m from the foot to the top (over the side's top, a railing's
        // cap, the top's carried on up), and its widest across the ladder's width between them.
        private static List<Vector2> Profile(KwShipLadderShip ship, float x, float side, float foot, float top, out float widest)
        {
            List<Vector2> profile = new List<Vector2>();
            int steps = Mathf.Max(1, Mathf.CeilToInt((top - foot) / 0.1f));
            widest = 0f;
            for (int i = 0; i <= steps; i++)
            {
                float z = Mathf.Lerp(foot, top, i / (float)steps), middle = Outside(ship, x, side, z);
                foreach (float along in new[] { -KwShipLadderRoom.Half, KwShipLadderRoom.Half })
                    widest = Mathf.Max(widest, Outside(ship, x + along, side, z));
                widest = Mathf.Max(widest, middle);
                profile.Add(new Vector2(z, middle > 0f || profile.Count == 0 ? middle : profile[profile.Count - 1].y));
            }
            return profile;
        }
    }
}
