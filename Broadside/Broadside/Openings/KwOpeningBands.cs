using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An opening cut into level bands, all of one height and about <see cref="Step"/> high, as the sea lets itself in
    /// through it on a launched ship (<see cref="KwShipOpeningPlan"/>): each band's middle (u along, z up: the middle of
    /// its area), its area in m² and its foot's height. So the water comes in through the low bands of a tall doorway once
    /// the sea passes its sill, and through more of it as the sea rises. Pure.
    /// </summary>
    public static class KwOpeningBands
    {
        /// <summary>How high a band is at most.</summary>
        public const float Step = 0.25f;

        /// <summary>An opening's bands from its sill up (one for an opening no higher than a step).</summary>
        public static List<(Vector2 Middle, float Area, float Foot)> Of(KwOpening o)
        {
            List<(Vector2 Middle, float Area, float Foot)> bands = new List<(Vector2 Middle, float Area, float Foot)>();
            List<Vector2> outline = o.Outline();
            int count = Mathf.Max(1, Mathf.CeilToInt(o.Height / Step - 0.01f));
            float high = o.Height / count;
            for (int k = 0; k < count; k++)
            {
                float foot = o.Sill + k * high;
                List<Vector2> band = Clip(Clip(outline, foot, 1f), foot + high, -1f);
                float area = KwOpeningShape.Signed(band);
                if (Mathf.Abs(area) > 1e-5f)
                    bands.Add((Middle(band, area), Mathf.Abs(area), foot));
            }
            return bands;
        }

        // A convex outline cut at a height: the part above it (way 1) or below it (way -1).
        private static List<Vector2> Clip(List<Vector2> ring, float z, float way)
        {
            List<Vector2> kept = new List<Vector2>();
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], c = ring[(i + 1) % ring.Count];
                float da = way * (a.y - z), dc = way * (c.y - z);
                if (da >= 0f)
                    kept.Add(a);
                if (da * dc < 0f)
                    kept.Add(Vector2.Lerp(a, c, da / (da - dc)));
            }
            return kept;
        }

        // The middle of a polygon's area (its signed area given).
        private static Vector2 Middle(List<Vector2> ring, float signed)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], c = ring[(i + 1) % ring.Count];
                sum += (a + c) * (a.x * c.y - c.x * a.y);
            }
            return sum / (6f * signed);
        }
    }
}
