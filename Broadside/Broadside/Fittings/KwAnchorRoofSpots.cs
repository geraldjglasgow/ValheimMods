using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The places an anchor windlass standing on a cabin's roof (<see cref="KwAnchorShip.Roof"/>) may lead its cable over the
    /// side (<see cref="KwAnchorSpot"/>, in the windlass's kit frame): along each of that roof's sides at the hull, where the
    /// roof's edge is the ship's side (the cabin's raised side, the hull carried up from its sheer), every
    /// <see cref="Step"/> and snug either side of each post of its roof railing: a chock seated on the railing's cap and
    /// reaching out over the raised side's top (the cable over the cap between two posts, under the top rail), or seated on
    /// the raised side's top where no rail stands. The main deck's rule kept (<see cref="KwAnchorSpots"/>): never on a post,
    /// where the roof is narrower than <see cref="Narrowest"/>, where the hull's own sheer is as high as the roof (its chocks
    /// are on the sheer there), or where the hung anchor would cover an opening in the raised side or the hull's side under it.
    /// </summary>
    public static class KwAnchorRoofSpots
    {
        // The step along the edge; the narrowest half width; how far the roof stands over the hull's sheer at the least.
        private const float Step = 0.1f, Narrowest = 0.4f, Raised = 0.1f;

        /// <summary>The chocks along the roof the windlass stands on, added to the others.</summary>
        public static void Add(KwAnchorShip ship, List<KwAnchorSpot> spots)
        {
            KwAnchorCabin? cabin = ship.Roof;
            if (cabin == null)
                return;
            foreach (float side in new[] { 1f, -1f })
            {
                if (!Side(cabin, side).AtHull)
                    continue;
                foreach (float x in Places(ship, cabin, side))
                {
                    if (Chock(ship, cabin, side, x) is KwAnchorSpot spot && Free(ship, cabin, side, x, spot))
                        spots.Add(spot);
                }
            }
        }

        /// <summary>
        /// A chock at x along the cabin (its kit) on one side (+1 its left), whatever is in the way: on the roof railing's cap
        /// where it stands there, else on the raised side's top; null where that side is not at the hull, the roof is too
        /// narrow there, the hull's sheer is not under the roof or no planking part is under it.
        /// </summary>
        public static KwAnchorSpot? Chock(KwAnchorShip ship, KwAnchorCabin cabin, float side, float x)
        {
            KwCabinSpec spec = cabin.Spec;
            float? edge = Edge(cabin, side, x);
            Vector2[] under = KwCabinOutline.HullAt(spec, x);
            if (!Side(cabin, side).AtHull || edge == null || edge.Value < Narrowest || under[under.Length - 1].y > spec.Top - Raised)
                return null;
            Vector3 rim = cabin.Pose.Out(new Vector3(x, side * edge.Value, spec.Top));
            KwAnchorHull? hull = ship.Hulls.Find(h => h.Pose.Local(rim).x >= h.From && h.Pose.Local(rim).x <= h.To);
            if (hull == null)
                return null;
            bool railed = ship.RoofRails.Exists(r => r.Covers(rim));
            float outer = KwShapes.WidthAt(KwCabinOutline.Up(spec, x).Half, spec.Top) + KwPlanks.Lap, inner = edge.Value - (railed ? 2f * KwRailing.Half : 0f);
            Vector3 at = cabin.Pose.Out(new Vector3(x, side * (inner + outer) / 2f, spec.Top + (railed ? KwRailing.CapTop : 0f)));
            return new KwAnchorSpot(hull, false, railed, at, Outward(cabin, side, x), (outer - inner) / 2f, (outer - inner) / 2f);
        }

        // The places along a side's edge tried: every step, and just clear of each roof railing post standing on that side.
        private static List<float> Places(KwAnchorShip ship, KwAnchorCabin cabin, float side)
        {
            List<float> places = new List<float>();
            float from = cabin.Spec.X0, to = cabin.Spec.X1;
            for (float x = from + Step; x <= to - Step; x += Step)
                places.Add(x);
            foreach (KwAnchorRoofRail rail in ship.RoofRails)
            {
                foreach (Vector3 post in rail.Posts())
                {
                    Vector3 k = cabin.Pose.Local(post);
                    if (Mathf.Sign(k.y) != side || Mathf.Abs(k.z - cabin.Spec.Top) > 0.15f)
                        continue;
                    foreach (float x in new[] { k.x - KwAnchorSpots.PostGap - 0.002f, k.x + KwAnchorSpots.PostGap + 0.002f })
                    {
                        if (x > from + Step / 2f && x < to - Step / 2f)
                            places.Add(x);
                    }
                }
            }
            return KwAnchorSpots.Apart(places);
        }

        // Clear of the roof railing's posts, and of the openings in the raised side and the hull's side under it that the hung
        // anchor would cover.
        private static bool Free(KwAnchorShip ship, KwAnchorCabin cabin, float side, float x, KwAnchorSpot spot)
        {
            Vector3 rim = cabin.Pose.Out(new Vector3(x, side * Edge(cabin, side, x)!.Value, cabin.Spec.Top));
            if (ship.RoofRails.Exists(r => r.NearPost(rim, KwAnchorSpots.PostGap)))
                return false;
            string raised = side > 0f ? KwOpening.RaisedLeft : KwOpening.RaisedRight;
            if (KwAnchorSpots.Opened(cabin.Spec.Openings, raised, x, cabin.Height(spot.At)))
                return false;
            Vector3 k = spot.Hull.Pose.Local(spot.At);
            return !KwAnchorSpots.Opened(spot.Hull.Openings, k.y >= 0f ? KwOpening.Port : KwOpening.Starboard, k.x, k.z);
        }

        // Level and out of the roof square to its edge at x (windlass kit).
        private static Vector3 Outward(KwAnchorCabin cabin, float side, float x)
        {
            float lo = x - 0.05f, hi = x + 0.05f, here = Edge(cabin, side, x) ?? 0f;
            float a = Edge(cabin, side, lo) ?? here, c = Edge(cabin, side, hi) ?? here;
            lo = Edge(cabin, side, lo) == null ? x : lo;
            hi = Edge(cabin, side, hi) == null ? x : hi;
            Vector2 n = new Vector2(hi - lo < 1e-4f ? 0f : -(c - a) / (hi - lo), side).normalized;
            Vector3 way = cabin.Pose.Way(new Vector3(n.x, n.y, 0f));
            return new Vector3(way.x, way.y, 0f).normalized;
        }

        // How far the roof's edge reaches on one side at x along the cabin; null past its ends.
        private static float? Edge(KwAnchorCabin cabin, float side, float x) =>
            cabin.RoofReach(x) is (float left, float right) ? side > 0f ? left : right : (float?)null;

        private static KwCabinSide Side(KwAnchorCabin cabin, float side) => side > 0f ? cabin.Spec.Left : cabin.Spec.Right;
    }
}
