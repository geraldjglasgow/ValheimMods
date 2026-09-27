using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Bed spawn points as text and the comparisons the feature makes. Text is "x:y:z", invariant, to the
    /// centimetre (no commas, so a point is one member of a <c>CharacterData</c> set). Two points less than a metre
    /// apart are the same bed, the game's own tolerance in <c>Bed.IsCurrent</c>. Nearest is measured on the map
    /// (x and z): a dungeon interior lies thousands of metres above its entrance, so height would only blur the order.
    /// </summary>
    public static class BedPoints
    {
        public const float SameBed = 1f;

        public static string Format(Vector3 point)
        {
            return Number(point.x) + ":" + Number(point.y) + ":" + Number(point.z);
        }

        public static bool TryParse(string text, out Vector3 point)
        {
            point = Vector3.zero;
            string[] parts = (text ?? "").Split(':');
            if (parts.Length != 3)
                return false;
            float[] values = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                    return false;
            }
            point = new Vector3(values[0], values[1], values[2]);
            return true;
        }

        public static bool Same(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < SameBed;

        public static bool Contains(List<Vector3> points, Vector3 point) => points.Exists(p => Same(p, point));

        public static float MapDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Nearest first, by map distance from <paramref name="from"/>.</summary>
        public static void SortByDistance(List<Vector3> points, Vector3 from)
        {
            points.Sort((a, b) => MapDistance(a, from).CompareTo(MapDistance(b, from)));
        }

        private static string Number(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
