using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Water in a launched ship's hull as its slices hold it (<see cref="KwHullSlices"/>, the sections it floats on): in a
    /// run of slices (one compartment of the hold, <see cref="KwFloodHolds"/>, or the whole hull) the water has one flat
    /// surface, a plane in the ship's frame, n·p = d, with n the way up it lies; each slice's section is wet under it
    /// (<see cref="KwSectionMath.Wet"/>, the same clipping the buoyancy uses), so in every slice the water lies level, on
    /// the low side when the ship heels. For a volume, d is found by halving. A run is its first slice and how many.
    /// </summary>
    public static class KwFloodLevel
    {
        private const int Halvings = 22;
        private const float Full = 1e5f;

        /// <summary>How much water the whole hull holds up to its sheer: every slice's whole section.</summary>
        public static float Capacity(List<KwHullSlice> slices) => Capacity(slices, 0, slices.Count);

        /// <summary>How much water a run of slices holds up to the sheer.</summary>
        public static float Capacity(List<KwHullSlice> slices, int first, int count)
        {
            float volume = 0f;
            for (int i = first; i < first + count && i < slices.Count; i++)
                volume += KwSectionMath.Wet(slices[i].Shape, 0f, 1f, Full, out _) * slices[i].Length;
            return volume;
        }

        /// <summary>The water under the plane n·p = d in the whole hull.</summary>
        public static float Volume(List<KwHullSlice> slices, Vector3 n, float d) => Volume(slices, 0, slices.Count, n, d);

        /// <summary>The water under the plane n·p = d in a run of slices.</summary>
        public static float Volume(List<KwHullSlice> slices, int first, int count, Vector3 n, float d)
        {
            float volume = 0f;
            for (int i = first; i < first + count && i < slices.Count; i++)
            {
                KwHullSlice s = slices[i];
                volume += KwSectionMath.Wet(s.Shape, n.x, n.y, d - n.z * s.Z, out _) * s.Length;
            }
            return volume;
        }

        /// <summary>The plane's d that holds a volume of water in the whole hull, its surface's way up n.</summary>
        public static float Solve(List<KwHullSlice> slices, Vector3 n, float volume) => Solve(slices, 0, slices.Count, n, volume);

        /// <summary>The plane's d that holds a volume of water in a run of slices, its surface's way up n.</summary>
        public static float Solve(List<KwHullSlice> slices, int first, int count, Vector3 n, float volume)
        {
            Range(slices, first, count, n, out float low, out float high);
            for (int i = 0; i < Halvings; i++)
            {
                float mid = (low + high) / 2f;
                if (Volume(slices, first, count, n, mid) < volume)
                    low = mid;
                else
                    high = mid;
            }
            return (low + high) / 2f;
        }

        /// <summary>Each slice's water under the plane in the whole hull: its area and the middle of it (x across, y up).</summary>
        public static void Shares(List<KwHullSlice> slices, Vector3 n, float d, float[] areas, Vector2[] middles) =>
            Shares(slices, 0, slices.Count, n, d, areas, middles);

        /// <summary>Each slice's water under the plane in a run of slices, into the slices' own places in the arrays.</summary>
        public static void Shares(List<KwHullSlice> slices, int first, int count, Vector3 n, float d, float[] areas, Vector2[] middles)
        {
            for (int i = first; i < first + count && i < slices.Count && i < areas.Length; i++)
            {
                KwHullSlice s = slices[i];
                areas[i] = KwSectionMath.Wet(s.Shape, n.x, n.y, d - n.z * s.Z, out Vector2 middle);
                middles[i] = middle;
            }
        }

        /// <summary>Whether a point of the ship's frame is inside the hull: within the section of the slice it stands in.</summary>
        public static bool Inside(List<KwHullSlice> slices, Vector3 local)
        {
            foreach (KwHullSlice s in slices)
            {
                if (Mathf.Abs(local.z - s.Z) <= s.Length / 2f)
                    return Within(s.Shape, new Vector2(local.x, local.y));
            }
            return false;
        }

        // Crossings of a ray from the point toward +x with the outline's edges: odd, inside.
        private static bool Within(Vector2[] shape, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
            {
                Vector2 a = shape[i], b = shape[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < a.x + (b.x - a.x) * (p.y - a.y) / (b.y - a.y))
                    inside = !inside;
            }
            return inside;
        }

        // The lowest and highest n·p over the run's outlines: the plane is dry at the one and full at the other.
        private static void Range(List<KwHullSlice> slices, int first, int count, Vector3 n, out float low, out float high)
        {
            (low, high) = (float.MaxValue, float.MinValue);
            for (int i = first; i < first + count && i < slices.Count; i++)
            {
                foreach (Vector2 p in slices[i].Shape)
                {
                    float at = n.x * p.x + n.y * p.y + n.z * slices[i].Z;
                    (low, high) = (Mathf.Min(low, at), Mathf.Max(high, at));
                }
            }
            if (low > high)
                (low, high) = (0f, 0f);
        }
    }
}
