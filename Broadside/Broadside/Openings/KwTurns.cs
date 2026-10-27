using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Turns worked out in plain arithmetic, as Unity's own would give them, so the openings' leaves can be laid out and
    /// checked away from the game: the turn whose forward and up are two ways (<see cref="Quaternion.LookRotation(Vector3, Vector3)"/>'s),
    /// and a turn about an axis (<see cref="Quaternion.AngleAxis"/>'s).
    /// </summary>
    public static class KwTurns
    {
        /// <summary>The turn taking +z to forward and +y as near up as it can, square to forward.</summary>
        public static Quaternion Look(Vector3 forward, Vector3 up)
        {
            Vector3 z = forward.normalized, x = Vector3.Cross(up, z).normalized, y = Vector3.Cross(z, x);
            float trace = x.x + y.y + z.z;
            if (trace > 0f)
            {
                float s = Mathf.Sqrt(trace + 1f) * 2f;
                return new Quaternion((y.z - z.y) / s, (z.x - x.z) / s, (x.y - y.x) / s, 0.25f * s);
            }
            if (x.x > y.y && x.x > z.z)
            {
                float s = Mathf.Sqrt(1f + x.x - y.y - z.z) * 2f;
                return new Quaternion(0.25f * s, (y.x + x.y) / s, (z.x + x.z) / s, (y.z - z.y) / s);
            }
            if (y.y > z.z)
            {
                float s = Mathf.Sqrt(1f + y.y - x.x - z.z) * 2f;
                return new Quaternion((y.x + x.y) / s, 0.25f * s, (z.y + y.z) / s, (z.x - x.z) / s);
            }
            float t = Mathf.Sqrt(1f + z.z - x.x - y.y) * 2f;
            return new Quaternion((z.x + x.z) / t, (z.y + y.z) / t, 0.25f * t, (x.y - y.x) / t);
        }

        /// <summary>The turn of some degrees about an axis.</summary>
        public static Quaternion About(float degrees, Vector3 axis)
        {
            Vector3 a = axis.normalized * Mathf.Sin(degrees * Mathf.Deg2Rad / 2f);
            return new Quaternion(a.x, a.y, a.z, Mathf.Cos(degrees * Mathf.Deg2Rad / 2f));
        }
    }
}
