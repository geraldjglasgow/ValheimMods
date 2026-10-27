using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cabins a captain's wheel on a roof is reached through (<see cref="KwGearClimb"/>): the cabin whose roof it stands
    /// on and each one under it down to the one standing on a deck, lowest first (<see cref="Chain"/>; a cabin on a cabin
    /// on a cabin at most); and what a shaft climbing and running in them must pass: their rooms (<see cref="KwGearRoom"/>),
    /// the room round every doorway and window in their walls and in the hull's planking (<see cref="KwGearKeepOut"/>), the
    /// ship's masts and ladders, and anchor windlasses standing on their floors. In the rudder's kit frame, from the ship's
    /// pieces alone (<see cref="KwGearShip"/>), so every peer works out the same.
    /// </summary>
    internal sealed class KwGearRooms
    {
        // How near a roof's top a cabin's floor must be to stand on it; a windlass's room round it and its height; how far
        // round its corner a bevel box reaches; the most cabins climbed through.
        private const float Level = 0.06f, Windlass = 0.55f, WindlassHigh = 0.95f, Box = 0.12f;
        private const int Most = 4;

        private readonly KwGearShip ship;
        private readonly List<KwGearKeepOut> keep = new List<KwGearKeepOut>();
        private readonly List<(Vector2 At, float Foot, float Top)> windlasses = new List<(Vector2 At, float Foot, float Top)>();
        private readonly List<KwGearCeiling> ceilings = new List<KwGearCeiling>();

        private KwGearRooms(KwGearShip ship, List<KwGearRoom> chain)
        {
            (this.ship, Chain) = (ship, chain);
            foreach (KwGearRoom room in chain)
                ceilings.Add(new KwGearCeiling(this, room));
            foreach (KwGearCabin cabin in ship.Cabins)
            {
                if (chain.Exists(r => r.Cabin == cabin))
                    KwGearKeepOut.Of(cabin.Pose, KwOpeningSurfaces.Cabin(cabin.Spec), keep);
            }
            foreach ((KwGearPose pose, string prefab, string saved) in ship.Others)
                Take(pose, prefab, saved);
        }

        /// <summary>The cabins from the one on a deck up to the one the wheel stands on.</summary>
        public List<KwGearRoom> Chain { get; }

        /// <summary>The lowest cabin's floor: the deck the shaft comes forward under.</summary>
        public float Floor => Chain[0].Floor;

        /// <summary>The way under a cabin's roof in the chain.</summary>
        public KwGearCeiling Ceiling(int i) => ceilings[i];

        /// <summary>The cabins a wheel at a point stands over, when it stands on a cabin's roof (the highest roof it is on); null when it does not.</summary>
        public static KwGearRooms? Under(KwGearShip ship, Vector3 wheel)
        {
            List<KwGearRoom> rooms = ship.Cabins.FindAll(c => c.Spec.Unbuilt == null).ConvertAll(c => new KwGearRoom(c));
            KwGearRoom? top = null;
            foreach (KwGearRoom room in rooms)
            {
                if (room.OnRoof(wheel) && (top == null || room.Top > top.Top))
                    top = room;
            }
            if (top == null)
                return null;
            List<KwGearRoom> chain = new List<KwGearRoom> { top };
            for (KwGearRoom? below = Below(rooms, top); below != null && chain.Count < Most; below = Below(rooms, below))
                chain.Insert(0, below);
            return new KwGearRooms(ship, chain);
        }

        /// <summary>
        /// Whether a point of a climbing or a roof's shaft is clear by a margin: in the room of the cabin it is in (by height),
        /// out of every opening's room; a climbing shaft going on up through its roof, by its walls only. In a roof between two
        /// rooms, or under the lowest floor (the deck's way), it is.
        /// </summary>
        public bool Clear(Vector3 p, float margin, bool through = false)
        {
            KwGearRoom? room = Chain.Find(r => p.z >= r.Floor && p.z < r.Under);
            if (room == null)
                return true;
            return room.Free(p, margin, through) && !ByOpening(p, margin);
        }

        /// <summary>Whether a point is in the room round a doorway or a window, or within a margin of it.</summary>
        public bool ByOpening(Vector3 p, float margin)
        {
            foreach (KwGearKeepOut k in keep)
            {
                if (k.Holds(p, margin))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Whether the bevel box at the foot of the first climb, under the deck, lies inside the hull's planking all round
        /// (at a narrow stern the climb moves forward until it does).
        /// </summary>
        public bool Inboard(Vector3 foot)
        {
            foreach (float dx in new[] { -Box, 0f, Box })
            {
                foreach (float dz in new[] { -Box, 0f, Box })
                {
                    if (Mathf.Abs(foot.y) + Box > ship.Inside(new Vector3(foot.x + dx, foot.y, foot.z + dz)))
                        return false;
                }
            }
            return true;
        }

        /// <summary>Whether a straight stretch passes by a mast, a ladder or an anchor windlass standing at its height.</summary>
        public bool Blocked(Vector3 a, Vector3 b)
        {
            if (ship.Blocked(a, b))
                return true;
            float low = Mathf.Min(a.z, b.z), high = Mathf.Max(a.z, b.z);
            foreach ((Vector2 at, float foot, float top) in windlasses)
            {
                if (high >= foot && low <= top && Across(at, new Vector2(a.x, a.y), new Vector2(b.x, b.y)) < Windlass)
                    return true;
            }
            return false;
        }

        // The cabin a cabin stands on: one whose roof's top is at its floor and is over its middle.
        private static KwGearRoom? Below(List<KwGearRoom> rooms, KwGearRoom upper)
        {
            Vector3 middle = upper.Point(upper.Length / 2f, 0f, upper.Floor);
            return rooms.Find(r => r != upper && Mathf.Abs(r.Top - upper.Floor) < Level && r.Covers(middle.x, middle.y));
        }

        // A planking part's openings, or an anchor windlass's room.
        private void Take(KwGearPose pose, string prefab, string saved)
        {
            if (KwAnchor.Is(prefab))
            {
                Vector3 foot = KwMesh.ToKit(pose.Position);
                windlasses.Add((new Vector2(foot.x, foot.y), foot.z, foot.z + WindlassHigh));
            }
            else
                KwGearKeepOut.Of(pose, KwOpeningSurfaces.Of("planking", saved), keep);
        }

        // How far a point is from the segment p to q (seen from above).
        private static float Across(Vector2 at, Vector2 p, Vector2 q)
        {
            Vector2 run = q - p;
            float t = run.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(at - p, run) / run.sqrMagnitude);
            return (p + run * t - at).magnitude;
        }
    }
}
