using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The shapes of the hold's water as it is drawn (<see cref="KwFloodSurface"/>), in the ship's frame: where a
    /// compartment's surface meets a cross-section of the hull (<see cref="Chord"/>), and at a bulkhead between two
    /// compartments whose water stands at different levels, the water's end against it (<see cref="Face"/>): the hull's
    /// section there between the two surfaces (down to the hull's bottom when the other side is dry). Both drawn in a
    /// little from the planking. Pure arithmetic, no Unity objects.
    /// </summary>
    public static class KwFloodFaces
    {
        private const float Inset = 0.1f, Lift = 0.01f, Narrowest = 0.15f;
        private static readonly List<Vector2> Kept = new List<Vector2>(), Cut = new List<Vector2>();

        /// <summary>
        /// Where the water's surface meets a section at z: the ends of the line up·p = level across it, each drawn in by
        /// <see cref="Inset"/>; false where the section is dry there or the water too narrow.
        /// </summary>
        public static bool Chord(Vector2[] shape, float z, Vector3 up, float level, out Vector3 port, out Vector3 starboard)
        {
            (port, starboard) = (Vector3.zero, Vector3.zero);
            float c = level - up.z * z, low = float.MaxValue, high = float.MinValue;
            Vector2 along = new Vector2(up.y, -up.x).normalized, lowAt = Vector2.zero, highAt = Vector2.zero;
            for (int i = 0; i < shape.Length; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Length];
                float dp = up.x * p.x + up.y * p.y - c, dq = up.x * q.x + up.y * q.y - c;
                if ((dp < 0f) == (dq < 0f))
                    continue;
                Vector2 cross = Vector2.Lerp(p, q, dp / (dp - dq));
                float t = Vector2.Dot(cross, along);
                if (t < low)
                    (low, lowAt) = (t, cross);
                if (t > high)
                    (high, highAt) = (t, cross);
            }
            if (high - low < Narrowest + 2f * Inset)
                return false;
            Vector3 lift = up * Lift;
            port = new Vector3(lowAt.x + along.x * Inset, lowAt.y + along.y * Inset, z) + lift;
            starboard = new Vector3(highAt.x - along.x * Inset, highAt.y - along.y * Inset, z) + lift;
            return true;
        }

        /// <summary>
        /// The water's end at a bulkhead at z: the section there under the surface up·p = high and over up·p = low (no
        /// lower bound when <paramref name="floored"/> is false), drawn in toward its middle; its corners in order round it
        /// into <paramref name="corners"/> (none when there is too little).
        /// </summary>
        public static void Face(Vector2[] shape, float z, Vector3 up, float high, float low, bool floored, List<Vector3> corners)
        {
            corners.Clear();
            Kept.Clear();
            Kept.AddRange(shape);
            Clip(Kept, -up.x, -up.y, -(high - up.z * z));
            if (floored)
                Clip(Kept, up.x, up.y, low - up.z * z);
            if (Kept.Count < 3)
                return;
            Vector2 middle = Vector2.zero;
            foreach (Vector2 p in Kept)
                middle += p / Kept.Count;
            foreach (Vector2 p in Kept)
            {
                float d = Vector2.Distance(p, middle);
                Vector2 q = d > 2f * Inset ? Vector2.MoveTowards(p, middle, Inset) : Vector2.Lerp(p, middle, 0.5f);
                corners.Add(new Vector3(q.x, q.y, z));
            }
        }

        // The polygon's part where a·x + b·y >= c (one edge of Sutherland-Hodgman), in place.
        private static void Clip(List<Vector2> polygon, float a, float b, float c)
        {
            Cut.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p = polygon[i], q = polygon[(i + 1) % polygon.Count];
                float dp = a * p.x + b * p.y - c, dq = a * q.x + b * q.y - c;
                if (dp >= 0f)
                    Cut.Add(p);
                if ((dp >= 0f) != (dq >= 0f))
                    Cut.Add(Vector2.Lerp(p, q, dp / (dp - dq)));
            }
            polygon.Clear();
            polygon.AddRange(Cut);
        }
    }
}
