using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Cutting a polygon into bands of a measure that runs evenly along its sides (a girth round the hull, a height up a
    /// transom): the part between two values, the corners where a cut crosses a side blended from that side's ends. A
    /// polygon whose band is one piece (convex, or a hull's section cut across its height) stays one polygon.
    /// </summary>
    public static class KwClip
    {
        /// <summary>The part of a ring where measure lies between lo and hi.</summary>
        public static List<T> Between<T>(List<T> ring, Func<T, float> measure, Func<T, T, float, T> blend, float lo, float hi) =>
            Keep(Keep(ring, p => measure(p) - lo, blend), p => hi - measure(p), blend);

        // The part of a ring where over is not below 0 (Sutherland-Hodgman against one cut).
        private static List<T> Keep<T>(List<T> ring, Func<T, float> over, Func<T, T, float, T> blend)
        {
            List<T> kept = new List<T>();
            for (int i = 0; i < ring.Count; i++)
            {
                T a = ring[i], b = ring[(i + 1) % ring.Count];
                float da = over(a), db = over(b);
                if (da >= 0f)
                    kept.Add(a);
                if ((da > 0f && db < 0f) || (da < 0f && db > 0f))
                    kept.Add(blend(a, b, da / (da - db)));
            }
            return kept;
        }
    }
}
