using EarthWright.Brush;
using EarthWright.Core;

namespace EarthWright.Paths
{
    /// <summary>
    /// The ramp and road lines of the HUD block next to the crosshair: what is planned (length, rise, width), its slope
    /// figures, points and paint, the set height clicked points take, a steepness warning or a reason the Preview's own
    /// reason line does not already show, and what the next click or key does. Set through <see cref="HudText"/>, which
    /// the Preview module draws (above the cost line); cleared when the entry is left.
    /// </summary>
    public static class PathHud
    {
        private const string SummaryKey = "paths";
        private const string StatsKey = "paths.stats";
        private const string HeightKey = "paths.height";
        private const string ProblemKey = "paths.problem";
        private const string NextKey = "paths.next";
        private const int Order = 300;
        private const string Red = "#FF5A4A";
        private const string Yellow = "#FFD24A";

        /// <summary>Call after the preview status was reported, so a reason shown there is not repeated here.</summary>
        public static void Show(PathView view, string workProblem)
        {
            PathPlan plan = view.Plan != null && !view.Plan.TooShort ? view.Plan : null;
            HudText.Set(SummaryKey, Summary(view.Title, plan), Order);
            HudText.Set(StatsKey, plan != null ? Stats(plan) : null, Order + 1);
            HudText.Set(HeightKey, PathSelection.UsesSetHeight ? PathWords.Format("hud_set_height", BrushState.TargetHeight) : null, Order + 2);
            HudText.Set(ProblemKey, ProblemLine(plan, workProblem), Order + 3);
            HudText.Set(NextKey, view.Next, Order + 4);
        }

        public static void Clear()
        {
            HudText.Clear(SummaryKey);
            HudText.Clear(StatsKey);
            HudText.Clear(HeightKey);
            HudText.Clear(ProblemKey);
            HudText.Clear(NextKey);
        }

        /// <summary>The reason the plan cannot be built, as shown to the player; null for none or for a plan too short to show.</summary>
        public static string ShownProblem(PathPlan plan) => plan != null && !plan.TooShort ? plan.FirstProblem : null;

        private static string Summary(string title, PathPlan plan)
        {
            if (plan == null)
                return title;
            return title + ": " + PathWords.Format("hud_shape", plan.Line.Length, plan.Line.Rise, plan.Draft.Shape.Width);
        }

        private static string Stats(PathPlan plan)
        {
            return PathWords.Format("hud_stats", plan.MaxSlope, plan.MeanSlope, plan.PointCount, PathWords.PaintName(plan.Draft.Paint));
        }

        /// <summary>The plan's (or protection's or the cost's) reason in red unless the preview's reason line shows it already, else the warning in yellow.</summary>
        private static string ProblemLine(PathPlan plan, string workProblem)
        {
            string problem = ShownProblem(plan) ?? workProblem;
            if (!string.IsNullOrEmpty(problem))
                return problem == PreviewStatus.FirstReason ? null : Coloured(problem, Red);
            return plan?.Warning != null ? Coloured(plan.Warning, Yellow) : null;
        }

        private static string Coloured(string text, string colour) => "<color=" + colour + ">" + text + "</color>";
    }
}
