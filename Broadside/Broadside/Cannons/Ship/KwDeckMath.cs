using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon on a launched ship's deck, in the ship's own frame (Unity axes: x across, y up, z along): where its
    /// origin is (the middle of its carriage, on the deck) and the way its muzzle points, level in the ship's frame.
    /// </summary>
    public struct KwDeckPose
    {
        public Vector3 At;
        public Vector3 Ahead;
    }

    /// <summary>
    /// A ship cannon hauled about a deck (<see cref="KwDeckHaul"/>), in plain arithmetic (no engine calls, so it is checked
    /// offline), all in the ship's frame. It is drawn after its hauler as a cart is (<see cref="Follow"/>: its middle rolls
    /// only along its way, never sideways, its rear at the handle's reach behind it, so a pull from the side turns it about
    /// its middle), no further a step than so far nor round than so much; it stands on its four trucks' feet
    /// (<see cref="Feet"/>), tilted along and across as the deck under them is (<see cref="Rest"/>), never further than
    /// <see cref="MostTilt"/>, and each truck steps no more than <see cref="Climb"/> up or down at a time.
    /// </summary>
    public static class KwDeckMath
    {
        /// <summary>How far a truck may step up or down from one pose to the next, metres; the most the carriage tilts, degrees.</summary>
        public const float Climb = 0.06f, MostTilt = 20f;

        /// <summary>
        /// The pose drawn after its hauler (his feet in the ship's frame) with its rear the handle's <paramref name="reach"/>
        /// behind its middle, its middle moved no more than <paramref name="most"/> metres and turned no more than
        /// <paramref name="turn"/> radians (both cut down in proportion); its height left for the deck to give.
        /// </summary>
        public static KwDeckPose Follow(in KwDeckPose pose, Vector3 hauler, float reach, float most, float turn)
        {
            KwCart cart = new KwCart { Axle = new Vector3(pose.At.x, 0f, pose.At.z), Ahead = pose.Ahead };
            KwCartMath.Follow(ref cart, hauler, reach);
            KwDeckPose to = new KwDeckPose { At = new Vector3(cart.Axle.x, pose.At.y, cart.Axle.z), Ahead = cart.Ahead };
            return Limit(pose, to, most, turn);
        }

        /// <summary>A move cut down to so many metres and radians, both in proportion.</summary>
        public static KwDeckPose Limit(in KwDeckPose from, in KwDeckPose to, float most, float turn)
        {
            Vector3 move = new Vector3(to.At.x - from.At.x, 0f, to.At.z - from.At.z);
            float angle = Angle(from.Ahead, to.Ahead), metres = move.magnitude, share = 1f;
            if (metres > most)
                share = most / metres;
            if (Mathf.Abs(angle) * share > turn)
                share = turn / Mathf.Abs(angle);
            if (share >= 1f)
                return to;
            return new KwDeckPose { At = from.At + move * share, Ahead = Turned(from.Ahead, angle * share) };
        }

        /// <summary>The angle from one level way to another, radians, toward the left (as <see cref="KwCartMath.Side"/>).</summary>
        public static float Angle(Vector3 from, Vector3 to) => Mathf.Atan2(Vector3.Dot(to, KwCartMath.Side(from)), Vector3.Dot(to, from));

        /// <summary>A level way turned so many radians toward its left.</summary>
        public static Vector3 Turned(Vector3 ahead, float radians) => ahead * Mathf.Cos(radians) + KwCartMath.Side(ahead) * Mathf.Sin(radians);

        /// <summary>Whether two poses are the same (within a tenth of a millimetre and a hundredth of a degree).</summary>
        public static bool Same(in KwDeckPose a, in KwDeckPose b) =>
            new Vector3(a.At.x - b.At.x, 0f, a.At.z - b.At.z).sqrMagnitude < 1e-8f && Mathf.Abs(Angle(a.Ahead, b.Ahead)) < 2e-4f;

        /// <summary>The four trucks' feet, level at the pose's height: front left, front right, rear left, rear right.</summary>
        public static void Feet(in KwDeckPose pose, KwShipSpec ship, Vector3[] into)
        {
            Vector3 left = KwCartMath.Side(pose.Ahead) * ship.TruckMid;
            Vector3 front = pose.At + pose.Ahead * ship.FrontAxle, rear = pose.At + pose.Ahead * ship.RearAxle;
            (into[0], into[1], into[2], into[3]) = (front + left, front - left, rear + left, rear - left);
        }

        /// <summary>
        /// The cannon's origin and rotation from the deck's heights under its four feet (as <see cref="Feet"/> orders them):
        /// its middle at their mean, tilted along and across as they are; false when that is more than
        /// <see cref="MostTilt"/> either way.
        /// </summary>
        public static bool Rest(in KwDeckPose pose, float[] heights, KwShipSpec ship, out Vector3 at, out Quaternion turn)
        {
            float along = (heights[0] + heights[1] - heights[2] - heights[3]) / (2f * (ship.FrontAxle - ship.RearAxle));
            float across = (heights[0] + heights[2] - heights[1] - heights[3]) / (4f * ship.TruckMid);
            at = new Vector3(pose.At.x, (heights[0] + heights[1] + heights[2] + heights[3]) / 4f, pose.At.z);
            Vector3 f = (pose.Ahead + Vector3.up * along).normalized, right = (-KwCartMath.Side(pose.Ahead) - Vector3.up * across).normalized;
            Vector3 u = Vector3.Cross(f, right).normalized;
            turn = KwCartMath.Rotation(Vector3.Cross(u, f), u, f);
            float most = Mathf.Tan(MostTilt * Mathf.Deg2Rad);
            return Mathf.Abs(along) <= most && Mathf.Abs(across) <= most;
        }

        /// <summary>A pose from the piece's own under the ship: its origin, and its forward laid level.</summary>
        public static KwDeckPose From(Vector3 at, Vector3 forward)
        {
            Vector3 level = new Vector3(forward.x, 0f, forward.z);
            return new KwDeckPose { At = at, Ahead = level.sqrMagnitude > 1e-8f ? level.normalized : Vector3.forward };
        }

        /// <summary>Where its rear's handle is (the hauler's feet when he holds it), level at the pose's height.</summary>
        public static Vector3 Handle(in KwDeckPose pose, KwShipSpec ship) => pose.At + pose.Ahead * KwMesh.ToUnity(ship.Handle).z;
    }
}
