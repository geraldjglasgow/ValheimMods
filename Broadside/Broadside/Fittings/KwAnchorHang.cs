using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a weighed anchor hangs from its fairlead (<see cref="KwAnchorSpot"/>): by its ring, <see cref="Drop"/> under where
    /// the cable leaves the fairlead, its stock along the hull and its arms square to it (or turned a quarter), plumb or,
    /// where the hull below would meet an arm, leaning out against it (its crown swung out from the hull, at most
    /// <see cref="Lean"/> degrees, as an anchor resting on the side hangs), and only where that is not enough held out from
    /// the hull on an iron arm (an anchor davit, at most <see cref="Farthest"/>): the least arm, then the least lean, for
    /// every outermost point of the anchor (<see cref="KwAnchorShapes.Points"/>) and the cable down to it to be clear of the
    /// hull's girth at its height, the railings, the cabins and the fittings (<see cref="KwAnchorShip"/>), and the cable let
    /// go plumb from there down past the keel clear of the hull where it bulges out below (a launched ship's anchor lies on
    /// the sea floor right under it). The windlass's kit frame.
    /// </summary>
    public static class KwAnchorHang
    {
        /// <summary>How far under the cable's leaving point the ring hangs; the longest arm; the arm's steps; the most lean.</summary>
        public const float Drop = 0.32f, Farthest = 0.6f, Steps = 0.05f, Lean = 30f;

        // How far every point of the anchor keeps from the hull and anything else; the lean's steps.
        private const float Clearance = 0.04f, LeanStep = 6f;

        private static List<Vector3>? points;

        /// <summary>The anchor hung from a fairlead: its arm's reach, its ring, its stock's and its shank's ways; null with why when it cannot hang clear.</summary>
        public static (float Reach, Vector3 Ring, Vector3 Stock, Vector3 Up)? Find(KwAnchorShip ship, KwAnchorSpot spot, out string? why)
        {
            why = null;
            float keel = Keel(spot);
            for (float reach = 0f; reach <= Farthest + 0.001f; reach += Steps)
            {
                Vector3 end = spot.Outer + spot.Outward * reach, ring = end - new Vector3(0f, 0f, Drop);
                string? cable = (reach > 0f ? KwAnchorRoute.Run(ship, spot.Outer, end, true, false) : null) ?? KwAnchorRoute.Run(ship, end, ring, true, false)
                    ?? KwAnchorRoute.Run(ship, ring, new Vector3(ring.x, ring.y, Mathf.Min(keel, ring.z)), true, false);
                if (cable != null)
                {
                    why ??= cable;
                    continue;
                }
                (Vector3 Stock, Vector3 Up)? hung = Hung(ship, spot, ring, ref why);
                if (hung != null)
                    return (reach, ring, hung.Value.Stock, hung.Value.Up);
            }
            return null;
        }

        // How deep the cable let go runs plumb past the hull under a fairlead: to the keel line of its planking there (windlass kit).
        private static float Keel(KwAnchorSpot spot)
        {
            KwAnchorHull hull = spot.Hull;
            return hull.Pose.Out(new Vector3(hull.Pose.Local(spot.At).x, 0f, hull.Bottom)).z;
        }

        // The least lean, either way round, at which the anchor on a ring is clear.
        private static (Vector3 Stock, Vector3 Up)? Hung(KwAnchorShip ship, KwAnchorSpot spot, Vector3 ring, ref string? why)
        {
            for (float lean = 0f; lean <= Lean + 0.001f; lean += LeanStep)
            {
                float a = lean * Mathf.Deg2Rad;
                Vector3 up = new Vector3(0f, 0f, Mathf.Cos(a)) - spot.Outward * Mathf.Sin(a);
                Vector3 outward = spot.Outward * Mathf.Cos(a) + new Vector3(0f, 0f, Mathf.Sin(a));
                foreach (Vector3 stock in new[] { spot.Along, outward })
                {
                    string? blocked = Anchor(ship, ring, stock, up);
                    if (blocked == null)
                        return (stock, up);
                    why ??= blocked;
                }
            }
            return null;
        }

        /// <summary>What is in the way of the anchor hung by its ring at a point, its stock and its shank (up) along these ways (null: nothing).</summary>
        public static string? Anchor(KwAnchorShip ship, Vector3 ring, Vector3 stock, Vector3 up)
        {
            Vector3 arms = Vector3.Cross(up, stock).normalized;
            List<KwAnchorSolid> near = ship.Near(ring, ring - up * KwAnchorShapes.Length, 0.5f + Clearance);
            foreach (Vector3 k in points ??= KwAnchorShapes.Points())
            {
                string? blocker = KwAnchorShip.Blocker(ring + stock * k.x + arms * k.y + up * k.z, Clearance, true, near);
                if (blocker != null)
                    return blocker;
            }
            return null;
        }
    }
}
