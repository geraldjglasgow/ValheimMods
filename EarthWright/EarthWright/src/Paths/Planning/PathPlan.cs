using System.Collections.Generic;

namespace EarthWright.Paths
{
    /// <summary>
    /// A draft planned against the terrain as this machine sees it: the centre line, every vertex that will change with
    /// its target and weight, the slope figures, and what (if anything) stops it from being built. Recomputed for the
    /// preview several times a second and once more, fresh, when the player builds.
    /// </summary>
    public sealed class PathPlan
    {
        public readonly PathDraft Draft;

        public readonly CentreLine Line;

        /// <summary>The vertices with their terrain; empty when the plan is too long or too big to read.</summary>
        public readonly List<PlannedVertex> Vertices = new List<PlannedVertex>();

        /// <summary>How many vertices the plan would change (also when too many to read).</summary>
        public int PointCount;

        /// <summary>Vertices no loaded heightmap covers.</summary>
        public int Unloaded;

        public float MaxSlope;

        public float MeanSlope;

        /// <summary>Shorter than the least length; nothing is shown for it.</summary>
        public bool TooShort;

        public bool TooLong;

        /// <summary>A cap refuses it (steepness, length, size, unloaded ground); null when none does.</summary>
        public string Problem;

        /// <summary>It would go past the height limits (a privileged edit that ignores the limits may still build it).</summary>
        public string LimitProblem;

        /// <summary>Allowed, but worth a word (steeper than the warning slope).</summary>
        public string Warning;

        public PathPlan(PathDraft draft, CentreLine line)
        {
            Draft = draft;
            Line = line;
        }

        public bool Valid => Problem == null && LimitProblem == null;

        public string FirstProblem => Problem ?? LimitProblem;
    }
}
