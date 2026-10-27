using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ladder's way down from a top meets on a launched ship (<see cref="KwSeaLadderDrop.Under"/>): the height it
    /// stops at, whether a ladder may stand there, and the solid it met (the hull's side, a fitting; null for a floor or for
    /// nothing within reach).
    /// </summary>
    public readonly struct KwSeaLadderFoot
    {
        public KwSeaLadderFoot(float y, bool stands, KwAnchorSolid? what) => (Y, Stands, What) = (y, stands, what);

        public float Y { get; }
        public bool Stands { get; }
        public KwAnchorSolid? What { get; }
    }

    /// <summary>
    /// What stands round a ladder the tool would build on a launched ship (<see cref="KwSeaLadderTool"/>), as the dock's
    /// rays find it (<see cref="KwLadderDrop"/>) but from the ship's blueprint (<see cref="KwSeaLadderShip"/>), in the
    /// ship's local frame. Straight down from just under its top, in front of the edge it hangs from: the first floor (a
    /// deck, a hold's floor, a cabin's roof, a hatch's coaming) within <see cref="KwLadder.MaxLength"/>, unless the hull's
    /// side or a fitting stands in the way first (a hatch's ladder stands on what it meets; an open edge's has nothing to
    /// stand on). And an open edge must be open there: nothing carrying on just beyond it, nothing standing on it just
    /// behind. Plain maths.
    /// </summary>
    public static class KwSeaLadderDrop
    {
        // How far apart the way down is looked at, and the room kept round it; the open edge's checks (KwLadderDrop's).
        private const float Step = 0.1f, Room = 0.04f, Carry = 0.35f, Stands = 0.25f, Beyond = 0.25f, Over = 0.5f, Above = 1.5f;

        /// <summary>
        /// Where a ladder with its top there would stand, the piece it hangs from left out. From an open edge (onFloor) only a
        /// floor (or a hatch's coaming) will do; from a hatch whatever it meets first.
        /// </summary>
        public static KwSeaLadderFoot Under(KwSeaLadderShip ship, Vector3 top, Vector3 facing, int leave, bool onFloor)
        {
            Vector3 from = top + facing * KwLadderDrop.Inset + Vector3.down * KwLadderDrop.Below;
            float lowest = from.y - KwLadder.MaxLength;
            float? floor = ship.Floor(from, from.y + 0.001f);
            bool found = floor.HasValue && floor.Value >= lowest;
            for (float y = from.y; y > (found ? floor!.Value : lowest) + Room; y -= Step)
            {
                KwAnchorSolid? what = ship.Blocked(new Vector3(from.x, y, from.z), Room, leave);
                if (what != null)
                    return new KwSeaLadderFoot(Meet(ship, from, Mathf.Min(from.y, y + Step), y, leave), !onFloor || KwSeaLadderSolids.Coaming(what), what);
            }
            return new KwSeaLadderFoot(found ? floor!.Value : lowest, found, null);
        }

        // Where the way down meets a solid, between a height clear of it and one in it, to a few millimetres (halving).
        private static float Meet(KwSeaLadderShip ship, Vector3 from, float clear, float hit, int leave)
        {
            for (int i = 0; i < 5; i++)
            {
                float middle = (clear + hit) / 2f;
                if (ship.Blocked(new Vector3(from.x, middle, from.z), Room, leave) != null)
                    hit = middle;
                else
                    clear = middle;
            }
            return hit - Room / 2f;
        }

        /// <summary>How long a ladder must be to reach what it stands on (0 for nothing it may stand on within reach).</summary>
        public static float Drop(KwSeaLadderShip ship, Vector3 top, Vector3 facing, int leave, bool onFloor)
        {
            KwSeaLadderFoot foot = Under(ship, top, facing, leave, onFloor);
            return foot.Stands ? top.y - foot.Y : 0f;
        }

        /// <summary>Why an open edge is not open where the ladder would stand; null when it is.</summary>
        public static string? Clear(KwSeaLadderShip ship, Vector3 top, Vector3 outward, int leave)
        {
            Vector3 beyond = top + outward * Beyond, behind = top - outward * Beyond;
            bool carried = ship.Floor(beyond, top.y + Over) is float on && on > top.y - Carry;
            if (carried || Column(ship, beyond, top.y + Over, top.y - Carry, leave))
                return "That edge is not open there: something carries on from it";
            if (Column(ship, behind, top.y + Above, top.y + Stands, leave))
                return "Something stands on the edge there";
            return null;
        }

        // Whether something stands in a column from one height down to another: a fitting, or a cabin's walls.
        private static bool Column(KwSeaLadderShip ship, Vector3 at, float high, float low, int leave)
        {
            for (float y = high; y >= low; y -= Step)
            {
                Vector3 p = new Vector3(at.x, y, at.z);
                KwAnchorSolid? solid = ship.Blocked(p, 0f, leave);
                if ((solid != null && !(solid is KwAnchorHull)) || ship.InCabin(p, 0f))
                    return true;
            }
            return false;
        }
    }
}
