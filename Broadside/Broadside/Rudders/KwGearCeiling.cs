using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The steering gear's way under a cabin's roof (<see cref="KwGearClimb"/>): the line shaft hung in hangers from the
    /// roof's boards, <see cref="KwGearTrain.Below"/> under its top as under a deck, inside the cabin's room clear of its
    /// walls, posts, doorways and windows, masts, ladders and windlasses (<see cref="KwGearRooms.Clear"/>). Its way from the
    /// top of the shaft climbing the cabin's back wall to a place under the roof (<see cref="Route"/>): nothing when the
    /// place is straight over it, one straight run, an L (forward first), else the fewest bends (<see cref="KwGearGrid"/>,
    /// its first bend no nearer the wall than <see cref="Shortest"/>). Kit axes of the rudder.
    /// </summary>
    internal sealed class KwGearCeiling : IKwGearWay
    {
        /// <summary>How far apart a stretch is checked; how far the shaft keeps from all round it; a bevel box's; the shortest stretch between two boxes.</summary>
        public const float Step = 0.2f, Margin = 0.09f, Box = 0.14f, Shortest = 0.35f;

        private readonly KwGearRooms rooms;
        private readonly KwGearRoom room;

        public KwGearCeiling(KwGearRooms rooms, KwGearRoom room) => (this.rooms, this.room) = (rooms, room);

        public float? At(float x, float y) => room.Covers(x, y) ? room.Top - KwGearTrain.Below : (float?)null;

        public float? Over(Vector3 p) => room.Covers(p.x, p.y) ? room.Top : (float?)null;

        public bool Run(Vector3 a, Vector3 b)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / Step));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)steps);
                if (Over(p) == null || !rooms.Clear(p, Margin))
                    return false;
            }
            return !rooms.Blocked(a, b);
        }

        public bool Corner(Vector3 p) => Over(p) != null && rooms.Clear(p, Box);

        /// <summary>The corners from a place under the roof to another (that one last, the first left out); null with no clear way.</summary>
        public List<Vector3>? Route(Vector3 s, Vector3 t)
        {
            Vector3 d = t - s;
            if (new Vector2(d.x, d.y).sqrMagnitude < 1e-4f)
                return new List<Vector3>();
            if (Lined(d) && Long(s, t) && Run(s, t))
                return new List<Vector3> { t };
            foreach (Vector3 m in new[] { new Vector3(t.x, s.y, s.z), new Vector3(s.x, t.y, s.z) })
            {
                if (Long(s, m) && Long(m, t) && Corner(m) && Run(s, m) && Run(m, t))
                    return new List<Vector3> { m, t };
            }
            List<Vector3>? grid = t.x - s.x >= 2f * Shortest ? KwGearGrid.Search(this, s, t, s.x + Shortest) : null;
            grid?.RemoveAt(0);
            return grid;
        }

        // Whether a run lies nearly along the ship or across it (no more than the straight run's skew off either).
        private static bool Lined(Vector3 d) =>
            Mathf.Abs(d.y) <= KwRudderGearRoute.Skew * Mathf.Abs(d.x) || Mathf.Abs(d.x) <= KwRudderGearRoute.Skew * Mathf.Abs(d.y);

        // Whether a stretch between two boxes leaves them room.
        private static bool Long(Vector3 a, Vector3 b) => (b - a).magnitude >= Shortest;
    }
}
