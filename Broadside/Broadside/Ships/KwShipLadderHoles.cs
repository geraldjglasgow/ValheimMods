using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An opening in a ship's side as its boarding ladder sees it (<see cref="KwShipLadderHoles"/>), in the ship's kit
    /// frame: its reach along the ship, its sill and top, the side it is on (+1 port), whether it is a doorway, and the
    /// cabin whose raised side it is cut in (null: the hull's planking).
    /// </summary>
    public readonly struct KwLadderHole
    {
        public KwLadderHole(float from, float to, float low, float high, float side, bool door, KwRigCabin? cabin)
        {
            (From, To, Low, High, Side, Door, Cabin) = (from, to, low, high, side, door, cabin);
        }

        public float From { get; }
        public float To { get; }
        public float Low { get; }
        public float High { get; }
        public float Side { get; }
        public bool Door { get; }
        public KwRigCabin? Cabin { get; }

        public float Middle => (From + To) / 2f;

        /// <summary>Whether it is on a side within a half width of a place along and between two heights.</summary>
        public bool Overlaps(float x, float half, float side, float low, float high) =>
            side == Side && From < x + half && To > x - half && Low < high && High > low;
    }

    /// <summary>
    /// The openings cut in a ship's sides, read from its pieces (<see cref="KwRigShip"/>): every planking part's on its two
    /// sides and every cabin's in its raised sides, only those that fit their wall (<see cref="KwOpeningSurfaces"/>), as
    /// they are cut on the ship. A cabin's own walls stand inside the hull and are left out.
    /// </summary>
    public static class KwShipLadderHoles
    {
        public static List<KwLadderHole> Of(KwRigShip rig)
        {
            List<KwLadderHole> holes = new List<KwLadderHole>();
            foreach (KwAnchorHull hull in rig.Hull.Hulls)
            {
                foreach (KwOpeningWall wall in KwOpeningSurfaces.Of("planking", KwShipParts.Saved(hull.Piece)))
                {
                    foreach (KwOpening o in wall.Openings)
                        holes.Add(Hole(hull.Pose, o, o.Wall == KwOpening.Port ? 1f : -1f, Mathf.Max(0.1f, hull.Sheer(o.U).x), null));
                }
            }
            foreach (KwRigCabin cabin in rig.Cabins)
            {
                foreach (KwOpening o in KwOpeningSurfaces.RaisedOpenings(cabin.Spec))
                    holes.Add(Hole(cabin.Pose, o, o.OnPort ? 1f : -1f, Across(cabin.Spec, o), cabin));
            }
            return holes;
        }

        // How far out a cabin's raised side is at an opening's sill.
        private static float Across(KwCabinSpec spec, KwOpening o) =>
            Mathf.Max(0.1f, KwShapes.WidthAt(KwCabinOutline.Up(spec, Mathf.Clamp(o.U, spec.X0, spec.X1)).Half, o.Sill));

        // An opening's corners from its part's kit (u along, its own side out, sill to top) into the ship's.
        private static KwLadderHole Hole(KwAnchorPose pose, KwOpening o, float side, float across, KwRigCabin? cabin)
        {
            Vector3 a = pose.Out(new Vector3(o.U0, side * across, o.Sill)), b = pose.Out(new Vector3(o.U1, side * across, o.Top));
            return new KwLadderHole(Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.z, b.z), Mathf.Sign(a.y + b.y), o.IsDoorway, cabin);
        }
    }
}
