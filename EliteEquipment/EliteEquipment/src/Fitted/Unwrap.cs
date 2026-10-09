using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// How the leggings' texture wraps round the body: three parts, the hips above the crotch and each leg below it, each
    /// round an upright axis through the middle of its covered triangles. Always made from the full length (waist to
    /// ankle), whatever the lower end, so the texture lies the same with boots on or off. The seam is at the back of the hips and on
    /// the inside of each leg, where it shows least. The corners of a triangle crossing the seam get angles on the same
    /// side, so its texture is not stretched round the whole leg.
    /// </summary>
    internal sealed class Unwrap
    {
        public const float Crotch = 0.84f;
        public const float Turn = 2f * Mathf.PI;

        // The seam's direction per part: the hips' back (-z); the left leg's inside (+x); the right leg's (-x).
        private static readonly float[] Seam = { Mathf.Atan2(-1f, 0f), 0f, Mathf.PI };

        private readonly Vector2[] centre = new Vector2[3];
        private readonly float[] radius = new float[3];

        public Unwrap(BodySurface body, bool[] covered)
        {
            List<Vector3>[] parts = Centres(body, covered);
            for (int k = 0; k < 3; k++)
            {
                if (parts[k].Count == 0)
                    continue;
                Vector2 sum = Vector2.zero;
                foreach (Vector3 p in parts[k])
                    sum += new Vector2(p.x, p.z);
                centre[k] = sum / parts[k].Count;
                float r = 0f;
                foreach (Vector3 p in parts[k])
                    r += (new Vector2(p.x, p.z) - centre[k]).magnitude;
                radius[k] = r / parts[k].Count;
            }
        }

        /// <summary>0 the hips, 1 the left leg (at negative x), 2 the right leg.</summary>
        public static int Part(Vector3 rest) => rest.y >= Crotch ? 0 : rest.x < 0f ? 1 : 2;

        /// <summary>The mean distance from the part's axis, metres.</summary>
        public float Radius(int part) => radius[part];

        /// <summary>The corners' angles from the part's seam, radians; continuous across the seam (up to two turns).</summary>
        public float[] Angles(int part, Vector3[] rest)
        {
            var angles = new float[rest.Length];
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < rest.Length; i++)
            {
                angles[i] = Angle(part, rest[i]);
                min = Mathf.Min(min, angles[i]);
                max = Mathf.Max(max, angles[i]);
            }
            if (max - min > Mathf.PI)
            {
                for (int i = 0; i < angles.Length; i++)
                    angles[i] += angles[i] < Mathf.PI ? Turn : 0f;
            }
            return angles;
        }

        private float Angle(int part, Vector3 rest)
        {
            float a = Mathf.Atan2(rest.z - centre[part].y, rest.x - centre[part].x) - Seam[part];
            a %= Turn;
            return a < 0f ? a + Turn : a;
        }

        private static List<Vector3>[] Centres(BodySurface body, bool[] covered)
        {
            var parts = new[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
            int[] t = body.Triangles;
            for (int i = 0; i < covered.Length; i++)
            {
                if (!covered[i])
                    continue;
                Vector3 c = (body.Rest[t[3 * i]] + body.Rest[t[3 * i + 1]] + body.Rest[t[3 * i + 2]]) / 3f;
                parts[Part(c)].Add(c);
            }
            return parts;
        }
    }
}
