using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How far a rudder's blade hangs below its rudder point (<see cref="KwRudderFit.Drop"/>): down to 15 cm over the
    /// ship's lowest keel bottom, never above the rudder point. The keel bottom is the lowest "Keel foot" of the keel the
    /// rudder hangs on and of every keel in line with it: within 30 m, on the same line (the same heading or reversed,
    /// less than half a metre to the side). A flat stern whose rudder point is at the keel bottom has no drop; a high
    /// stern's rudder hangs down to the keel bottom.
    /// </summary>
    public static class KwRudderDrop
    {
        public const float Clearance = 0.15f, Reach = 30f, Aside = 0.5f;

        /// <summary>The keel snap points the keel bottom is read from.</summary>
        public const string Foot = "Keel foot";
        private static readonly List<Piece> Near = new List<Piece>();

        /// <summary>The drop for a rudder point at a height over a keel bottom at another.</summary>
        public static float Of(float point, float bottom) => Mathf.Max(0f, point - (bottom + Clearance));

        /// <summary>Whether a keel (at a point on its line, running along a direction) is in line with another.</summary>
        public static bool InLine(Vector3 at, Vector3 along, Vector3 other, Vector3 otherAlong)
        {
            Vector3 a = Level(along), b = Level(otherAlong), off = other - at;
            off.y = 0f;
            if (off.magnitude > Reach || Mathf.Abs(Vector3.Dot(a, b)) < 0.995f)
                return false;
            return (off - a * Vector3.Dot(off, a)).magnitude < Aside;
        }

        /// <summary>The lowest keel bottom under a placed keel's rudder point: its own feet and those of the keels in line with it.</summary>
        public static float Bottom(Piece keel, Vector3 point)
        {
            float bottom = Lowest(keel, point.y);
            Near.Clear();
            Piece.GetAllPiecesInRadius(point, Reach, Near);
            foreach (Piece other in Near)
            {
                if (other != keel && KwKeel.IsKeel(other) && InLine(keel.transform.position, keel.transform.forward, other.transform.position, other.transform.forward))
                    bottom = Lowest(other, bottom);
            }
            return bottom;
        }

        // The lowest of a height and a keel's feet.
        private static float Lowest(Piece keel, float bottom)
        {
            foreach (Transform child in keel.transform)
            {
                if (child.name == Foot && child.CompareTag("snappoint"))
                    bottom = Mathf.Min(bottom, child.position.y);
            }
            return bottom;
        }

        private static Vector3 Level(Vector3 along)
        {
            along.y = 0f;
            return along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
        }
    }
}
