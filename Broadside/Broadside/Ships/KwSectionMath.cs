using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hull cross-section under water: the part of a closed outline (x across, y up) on the wet side of a straight
    /// water line a·x + b·y &lt; c, its area and the middle of that area (where the water lifts it), by clipping the
    /// outline at the line and the shoelace sums. And the outline's breadth at a level.
    /// </summary>
    public static class KwSectionMath
    {
        private static readonly Vector2[] clipped = new Vector2[256];

        /// <summary>The wet area of an outline under the line a·x + b·y = c, and its centroid (zero area: nothing wet).</summary>
        public static float Wet(Vector2[] shape, float a, float b, float c, out Vector2 centroid)
        {
            int n = Clip(shape, a, b, c);
            float area = 0f, cx = 0f, cy = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = clipped[i], q = clipped[(i + 1) % n];
                float cross = p.x * q.y - q.x * p.y;
                area += cross;
                cx += (p.x + q.x) * cross;
                cy += (p.y + q.y) * cross;
            }
            area *= 0.5f;
            centroid = Mathf.Abs(area) > 1e-6f ? new Vector2(cx, cy) / (6f * area) : Vector2.zero;
            return Mathf.Abs(area);
        }

        // The outline cut at the line (Sutherland-Hodgman, one edge), into the shared buffer; the corner count.
        private static int Clip(Vector2[] shape, float a, float b, float c)
        {
            int n = 0;
            for (int i = 0; i < shape.Length && n < clipped.Length - 2; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Length];
                float dp = c - (a * p.x + b * p.y), dq = c - (a * q.x + b * q.y);
                if (dp >= 0f)
                    clipped[n++] = p;
                if ((dp >= 0f) != (dq >= 0f))
                    clipped[n++] = Vector2.Lerp(p, q, dp / (dp - dq));
            }
            return n;
        }

        /// <summary>How wide the outline is at a level (between its outermost crossings of it).</summary>
        public static float Breadth(Vector2[] shape, float level)
        {
            float left = float.MaxValue, right = float.MinValue;
            for (int i = 0; i < shape.Length; i++)
            {
                Vector2 p = shape[i], q = shape[(i + 1) % shape.Length];
                if ((p.y - level) * (q.y - level) > 0f || Mathf.Approximately(p.y, q.y))
                    continue;
                float x = Mathf.Lerp(p.x, q.x, (level - p.y) / (q.y - p.y));
                (left, right) = (Mathf.Min(left, x), Mathf.Max(right, x));
            }
            return right > left ? right - left : 0f;
        }
    }
}
