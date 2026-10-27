using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A piece's pose on a launched ship (<see cref="KwShipRider"/>), plain arithmetic only (no engine calls, so it is
    /// checked offline): its place and turn in the ship's own frame from both world poses, the world pose back from them,
    /// and whether a pose has moved far enough to be written to the piece's ZDO again.
    /// </summary>
    public static class KwShipRiderPose
    {
        /// <summary>A unit quaternion's inverse (its conjugate).</summary>
        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        /// <summary>A quaternion scaled back to unit length (the identity for a zero one).</summary>
        public static Quaternion Unit(Quaternion q)
        {
            float length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return length < 1e-6f ? Quaternion.identity : new Quaternion(q.x / length, q.y / length, q.z / length, q.w / length);
        }

        /// <summary>A world pose in the frame of a ship standing at <paramref name="shipAt"/>, turned <paramref name="shipTurn"/>.</summary>
        public static (Vector3 At, Quaternion Turn) Local(Vector3 shipAt, Quaternion shipTurn, Vector3 at, Quaternion turn)
        {
            Quaternion back = Inverse(Unit(shipTurn));
            return (back * (at - shipAt), Unit(back * turn));
        }

        /// <summary>A pose in a ship's frame back in the world, the ship standing at <paramref name="shipAt"/>, turned <paramref name="shipTurn"/>.</summary>
        public static (Vector3 At, Quaternion Turn) World(Vector3 shipAt, Quaternion shipTurn, Vector3 local, Quaternion localTurn)
        {
            Quaternion turn = Unit(shipTurn);
            return (shipAt + turn * local, Unit(turn * localTurn));
        }

        /// <summary>
        /// A point on a moving body carried from one of its poses to another: the same point in the body's frame. A ray hits
        /// a ship's colliders at its physics pose while the ship (and everything on it) is drawn at its interpolated pose a
        /// little behind it, so a point hit is carried to where it is drawn.
        /// </summary>
        public static Vector3 Carry(Vector3 fromAt, Quaternion fromTurn, Vector3 toAt, Quaternion toTurn, Vector3 point) =>
            World(toAt, toTurn, Local(fromAt, fromTurn, point, Quaternion.identity).At, Quaternion.identity).At;

        /// <summary>A way (a normal) on a moving body carried from one of its turns to another.</summary>
        public static Vector3 CarryWay(Quaternion fromTurn, Quaternion toTurn, Vector3 way) =>
            Unit(toTurn) * (Inverse(Unit(fromTurn)) * way);

        /// <summary>The angle between two turns, degrees (from the turn between them, exact near none, where an arc cosine is not).</summary>
        public static float Degrees(Quaternion a, Quaternion b)
        {
            Quaternion d = Inverse(Unit(a)) * Unit(b);
            double sine = System.Math.Sqrt((double)d.x * d.x + (double)d.y * d.y + (double)d.z * d.z);
            return (float)(2.0 * System.Math.Atan2(sine, System.Math.Abs((double)d.w)) * 180.0 / System.Math.PI);
        }

        /// <summary>Whether a pose is further than so many metres or degrees from the one last written.</summary>
        public static bool Moved(Vector3 at, Quaternion turn, Vector3 written, Quaternion writtenTurn, float metres, float degrees) =>
            (at - written).sqrMagnitude > metres * metres || Degrees(turn, writtenTurn) > degrees;
    }
}
