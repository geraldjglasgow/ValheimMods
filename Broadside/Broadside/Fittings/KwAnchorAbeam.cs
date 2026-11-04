using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The lead of a windlass with no clear place to take its cable overboard (<see cref="KwAnchorPlan"/>): straight out
    /// abeam from its drum to the ship's side nearest it, whatever is in the way (the cable may pass through it), never to
    /// the bow. Its fairlead is a chock on the edge of the cabin roof it stands on where that edge is the ship's side
    /// (<see cref="KwAnchorRoofSpots"/>), a hawse pipe for a windlass inside a cabin where the side has room for one, else a
    /// chock on the sheer (<see cref="KwAnchorSpots"/>); the weighed anchor hangs there as clear of the hull as it can
    /// (<see cref="KwAnchorHang"/>), else plumb under the fairlead. The lead keeps why it is not clear, for the dock's hover
    /// (<see cref="KwAnchorDock"/>); the ship lets go and rides to its anchor there like any other lead. Plain maths from
    /// the ship's pieces, the same on every peer.
    /// </summary>
    public static class KwAnchorAbeam
    {
        // How far in from a planking part's ends the fairlead stays.
        private const float EndRoom = 0.1f;

        /// <summary>The lead straight out abeam, with why it is not clear; null with no hull round the windlass.</summary>
        public static KwAnchorLead? For(KwAnchorShip ship, string why)
        {
            KwAnchorHull? hull = Nearest(ship.Hulls);
            if (hull == null)
                return null;
            Vector3 k = hull.Pose.Local(Vector3.zero);
            float side = Side(hull, k);
            Vector3 away = hull.Pose.Way(new Vector3(0f, side, 0f));
            KwAnchorSpot spot = OnRoof(ship, away) ?? Spot(ship, hull, side, Mathf.Clamp(k.x, hull.From + EndRoom, hull.To - EndRoom));
            List<Vector3> deck = new List<Vector3> { KwAnchorRoute.Drum(spot.Inner.x >= 0f ? 1f : -1f, spot.Inner), spot.Inner };
            (float Reach, Vector3 Ring, Vector3 Stock, Vector3 Up) hang = KwAnchorHang.Find(ship, spot, out _)
                ?? (0f, spot.Outer - new Vector3(0f, 0f, KwAnchorHang.Drop), spot.Along, new Vector3(0f, 0f, 1f));
            return KwAnchorPlan.Kneed(ship, new KwAnchorLead(spot, deck, hang.Reach, hang.Ring, hang.Stock, hang.Up, why));
        }

        // The planking part the windlass stands over along its length, else the one whose end is nearest it.
        private static KwAnchorHull? Nearest(List<KwAnchorHull> hulls)
        {
            KwAnchorHull? best = null;
            float off = float.MaxValue;
            foreach (KwAnchorHull hull in hulls)
            {
                float x = hull.Pose.Local(Vector3.zero).x, past = Mathf.Max(0f, hull.From - x, x - hull.To);
                if (past < off)
                    (best, off) = (hull, past);
            }
            return best;
        }

        // The side (+1 the part's left) nearest the windlass: the one it stands to; on the middle line, the one it faces.
        private static float Side(KwAnchorHull hull, Vector3 k)
        {
            if (Mathf.Abs(k.y) > 0.05f)
                return Mathf.Sign(k.y);
            float facing = hull.Pose.Local(new Vector3(1f, 0f, 0f)).y - k.y;
            return facing < 0f ? -1f : 1f;
        }

        // A chock on the edge of the roof the windlass stands on, straight out from it, where that edge is the ship's side.
        private static KwAnchorSpot? OnRoof(KwAnchorShip ship, Vector3 away)
        {
            KwAnchorCabin? cabin = ship.Roof;
            if (cabin == null)
                return null;
            Vector3 k = cabin.Pose.Local(Vector3.zero);
            float side = cabin.Pose.Local(away).y - k.y < 0f ? -1f : 1f;
            return KwAnchorRoofSpots.Chock(ship, cabin, side, Mathf.Clamp(k.x, cabin.Spec.X0 + EndRoom, cabin.Spec.X1 - EndRoom));
        }

        // A hawse pipe for a windlass in a cabin where the side has room for one, else a chock on the sheer.
        private static KwAnchorSpot Spot(KwAnchorShip ship, KwAnchorHull hull, float side, float x) =>
            (ship.Housed ? KwAnchorSpots.PipeAt(ship, hull, side, x) : null) ?? KwAnchorSpots.Abeam(ship, hull, side, x);
    }
}
