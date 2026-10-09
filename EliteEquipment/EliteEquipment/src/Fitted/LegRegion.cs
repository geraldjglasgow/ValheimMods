using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// The body triangles fitted leggings cover: every triangle whose three corners lie between the leggings' lower end and the
    /// waist and are not moved by the arms, hands or head. These are the triangles the leggings are made of and the ones the
    /// body stops drawing while it is worn. The lower end is the ankle, or with boots on the highest of the body's edge
    /// loops at least <see cref="Tuck"/> below the boots' top, so the leggings reach into them and no skin shows between.
    /// </summary>
    internal static class LegRegion
    {
        /// <summary>Above the body's hip edge (1.08 to 1.11 m), the first above the belt line.</summary>
        public const float Waist = 1.12f;

        /// <summary>Below the body's ankle edge (0.095 to 0.124 m); the foot starts under it.</summary>
        public const float Ankle = 0.094f;

        public const float Tuck = 0.01f;

        // Heights closer than this belong to one edge loop round the leg.
        private const float LoopGap = 0.03f;

        public static bool[] Covered(BodySurface body, float bottom)
        {
            var covered = new bool[body.Triangles.Length / 3];
            for (int t = 0; t < covered.Length; t++)
            {
                covered[t] = Inside(body, body.Triangles[3 * t], bottom) && Inside(body, body.Triangles[3 * t + 1], bottom)
                    && Inside(body, body.Triangles[3 * t + 2], bottom);
            }
            return covered;
        }

        /// <summary>The leggings' lower end for boots reaching <paramref name="bootTop"/> metres (0 for none).</summary>
        public static float Bottom(BodySurface body, float bootTop)
        {
            float bottom = Ankle;
            if (bootTop <= Ankle)
                return bottom;
            foreach (Vector2 loop in Loops(body))
            {
                if (loop.y <= bootTop - Tuck && loop.x - 0.001f > bottom)
                    bottom = loop.x - 0.001f;
            }
            return bottom;
        }

        private static bool Inside(BodySurface body, int v, float bottom) =>
            !body.Upper[v] && body.Rest[v].y <= Waist && body.Rest[v].y >= bottom;

        /// <summary>The body's edge loops round the legs, as the lowest and highest height (x, y) of each.</summary>
        private static List<Vector2> Loops(BodySurface body)
        {
            var heights = new List<float>();
            foreach (Vector3 rest in body.Rest)
            {
                if (Mathf.Abs(rest.x) > 0.02f && rest.y >= Ankle && rest.y <= 0.8f)
                    heights.Add(rest.y);
            }
            heights.Sort();
            var loops = new List<Vector2>();
            foreach (float h in heights)
            {
                if (loops.Count == 0 || h - loops[loops.Count - 1].y > LoopGap)
                    loops.Add(new Vector2(h, h));
                else
                    loops[loops.Count - 1] = new Vector2(loops[loops.Count - 1].x, h);
            }
            return loops;
        }
    }
}
