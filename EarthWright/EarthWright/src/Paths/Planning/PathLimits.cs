using EarthWright.Protection;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Which planned vertices would end past the height limits, judged exactly as the engine will judge the edit on the
    /// owner: the same <see cref="LimitContext"/>, measured from the pre-edit base height, with the dig exceptions asked
    /// once at the centre of the edit's area and the admin range while an admin holds the limit override key (which
    /// makes the edit ignore the limits). A vertex already past a limit may stay or move back, never further out.
    /// </summary>
    public static class PathLimits
    {
        /// <summary>Metres a vertex may end past a limit before it counts, for rounding.</summary>
        private const float Tolerance = 0.05f;

        private static readonly LimitContext limits = new LimitContext();

        /// <summary>Flags the vertices past a limit (<see cref="PlannedVertex.PastLimit"/>) and returns how many there are.</summary>
        public static int Mark(PathPlan plan)
        {
            if (plan.Vertices.Count == 0)
                return 0;
            limits.Reset(AdminRouting.OverrideActive, HeightLimits.DigLifted(AreaCentre(plan)));
            int past = 0;
            for (int i = 0; i < plan.Vertices.Count; i++)
            {
                PlannedVertex vertex = plan.Vertices[i];
                if (!vertex.Loaded || !Past(vertex))
                    continue;
                vertex.PastLimit = true;
                plan.Vertices[i] = vertex;
                past++;
            }
            return past;
        }

        private static bool Past(PlannedVertex vertex)
        {
            limits.Range(vertex.X, vertex.Z, out float up, out float down);
            float final = vertex.Final;
            bool tooHigh = final > vertex.Base + up + Tolerance && final > vertex.Current + Tolerance;
            bool tooLow = final < vertex.Base - down - Tolerance && final < vertex.Current - Tolerance;
            return tooHigh || tooLow;
        }

        /// <summary>The centre of the vertices' bounds at height 0: where the engine asks the dig exceptions for a vertex edit.</summary>
        private static Vector3 AreaCentre(PathPlan plan)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
            foreach (PlannedVertex vertex in plan.Vertices)
            {
                minX = Mathf.Min(minX, vertex.X);
                maxX = Mathf.Max(maxX, vertex.X);
                minZ = Mathf.Min(minZ, vertex.Z);
                maxZ = Mathf.Max(maxZ, vertex.Z);
            }
            return new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        }
    }
}
