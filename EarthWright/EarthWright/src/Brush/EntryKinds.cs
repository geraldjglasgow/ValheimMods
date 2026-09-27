using EarthWright.Actions;

namespace EarthWright.Brush
{
    /// <summary>
    /// How the brush treats a selected entry. The ramp and road entries (special keys "ramp" and "road", the Paths
    /// module's) are lines of clicked points: the brush only gives them a width (the radius), a paint, a target height
    /// and the cursor, and leaves the shape and style keys to them (N cycles the ramp profile). Every other entry,
    /// special or not (clear, uproot, groundbreaker, custom entries), uses the whole brush: shape, rotation, hardness,
    /// style, target and aim-at-edge.
    /// </summary>
    public static class EntryKinds
    {
        public const string RampKey = "ramp";
        public const string RoadKey = "road";

        /// <summary>A ramp or road entry: its clicks are points, not strokes.</summary>
        public static bool IsPath(ToolAction action)
        {
            return action != null && (action.Special == RampKey || action.Special == RoadKey);
        }
    }
}
