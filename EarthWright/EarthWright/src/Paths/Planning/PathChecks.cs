using Translations;

namespace EarthWright.Paths
{
    /// <summary>
    /// The caps a ramp or road must meet (synced settings, so every player of a server is held to the same): at least a
    /// metre long, at most the longest length, the steepest slope and the most points, all on loaded ground, and not
    /// past the height limits. Each failed check leaves a reason on the plan, worded for the player.
    /// </summary>
    public static class PathChecks
    {
        /// <summary>The shortest ramp or road in metres.</summary>
        public const float MinLength = 1f;

        /// <summary>Length and slope, from the centre line alone.</summary>
        public static void CheckLine(PathPlan plan)
        {
            float length = plan.Line.Length;
            if (length < MinLength)
            {
                plan.TooShort = true;
                plan.Problem = Lang.Get("ew_paths_too_short");
                return;
            }
            if (length > PathSettings.MaxLength.Value)
            {
                plan.TooLong = true;
                plan.Problem = Lang.Format("ew_paths_too_long", PathWords.Number(length, "0"),
                    PathWords.Number(PathSettings.MaxLength.Value, "0"));
                return;
            }
            float max = PathSettings.MaxSlopeValue;
            if (plan.MaxSlope > max)
                plan.Problem = Lang.Format("ew_paths_too_steep", PathWords.Number(plan.MaxSlope, "0"), PathWords.Number(max, "0"));
            else if (plan.MaxSlope > PathSettings.WarningSlope.Value)
                plan.Warning = Lang.Format("ew_paths_steep_warning", PathWords.Number(PathSettings.WarningSlope.Value, "0"));
        }

        /// <summary>Size, loaded ground and height limits, after the vertices were planned.</summary>
        public static void CheckVertices(PathPlan plan)
        {
            int cap = PathSettings.MaxPoints.Value;
            if (plan.Problem == null && plan.PointCount > cap)
                plan.Problem = Lang.Format("ew_paths_too_big", plan.PointCount, cap);
            if (plan.Problem == null && plan.Unloaded > 0)
                plan.Problem = Lang.Get("ew_paths_unloaded");
            int past = PathLimits.Mark(plan);
            if (past > 0 && PathSettings.RefusePastLimit.Value)
                plan.LimitProblem = Lang.Format("ew_paths_past_limit", past);
        }
    }
}
