using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// "Continue the flat": finds the height earlier edits gave the ground around the crosshair. It samples the edited
    /// vertices within the brush radius (from this machine's copy of the terrain compilers, the same on every client),
    /// looks for the largest group of heights within the tolerance of each other, and answers its median, unless a
    /// second group of comparable size exists (two platforms that disagree) or too few vertices were edited. Cached for
    /// a short time, since the ground under a still crosshair does not change every frame.
    /// </summary>
    public static class ContinueFlat
    {
        private const int MinVertices = 3;
        private const float MaxSampleRadius = 30f;
        private const float CacheSeconds = 0.25f;

        private static readonly List<float> heights = new List<float>();
        private static Vector3Int cachedVertex;
        private static float cachedRadius = -1f;
        private static float cachedAt = -10f;
        private static bool cachedFound;
        private static float cachedHeight;

        public static bool TryHeight(Vector3 aim, float radius, out float height)
        {
            Vector3Int vertex = TerrainRead.VertexOf(aim);
            bool fresh = vertex == cachedVertex && Mathf.Approximately(radius, cachedRadius) && Time.unscaledTime - cachedAt < CacheSeconds;
            if (!fresh)
            {
                cachedVertex = vertex;
                cachedRadius = radius;
                cachedAt = Time.unscaledTime;
                cachedFound = Compute(aim, radius, out cachedHeight);
            }
            height = cachedHeight;
            return cachedFound;
        }

        private static bool Compute(Vector3 aim, float radius, out float height)
        {
            height = 0f;
            Sample(aim, Mathf.Clamp(radius, 1f, MaxSampleRadius));
            if (heights.Count < MinVertices)
                return false;
            heights.Sort();
            float tolerance = TargetSettings.ContinueTolerance.Value;
            int best = LargestGroup(tolerance, float.NaN, float.NaN, out int bestStart);
            if (best < MinVertices || best < heights.Count * 0.25f)
                return false;
            float low = heights[bestStart] - tolerance, high = heights[bestStart + best - 1] + tolerance;
            int rival = LargestGroup(tolerance, low, high, out _);
            if (rival >= MinVertices && rival * 2 >= best)
                return false;
            height = heights[bestStart + best / 2];
            return true;
        }

        /// <summary>Collects the current heights of edited vertices inside the radius, at most about 21 x 21 samples.</summary>
        private static void Sample(Vector3 aim, float radius)
        {
            heights.Clear();
            float step = Mathf.Max(1f, Mathf.Ceil(radius / 10f));
            for (float dx = -radius; dx <= radius; dx += step)
            {
                for (float dz = -radius; dz <= radius; dz += step)
                {
                    if (dx * dx + dz * dz > radius * radius)
                        continue;
                    if (TerrainRead.TryVertex(aim + new Vector3(dx, 0f, dz), out VertexInfo info) && info.HeightModified)
                        heights.Add(info.Current);
                }
            }
        }

        /// <summary>
        /// The largest run of sorted heights spanning at most twice the tolerance, ignoring heights inside
        /// [skipLow, skipHigh] (NaN: skip nothing). Two pointers over the sorted list.
        /// </summary>
        private static int LargestGroup(float tolerance, float skipLow, float skipHigh, out int bestStart)
        {
            bestStart = 0;
            int best = 0, start = 0;
            for (int end = 0; end < heights.Count; end++)
            {
                if (heights[end] >= skipLow && heights[end] <= skipHigh)
                {
                    start = end + 1;
                    continue;
                }
                while (heights[end] - heights[start] > tolerance * 2f)
                    start++;
                if (end - start + 1 > best)
                {
                    best = end - start + 1;
                    bestStart = start;
                }
            }
            return best;
        }
    }
}
