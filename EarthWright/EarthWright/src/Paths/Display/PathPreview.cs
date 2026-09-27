using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// Draws a ramp or road plan in the world for the local player only: the centre line and both surface edges at the
    /// planned heights, coloured segment by segment by slope (or red when it cannot be built), the end caps, a post on
    /// every placed point, the line to where the next waypoint would go, and optionally a dot on every changed point.
    /// Planning is local: nothing here touches the terrain or the network.
    /// </summary>
    public static class PathPreview
    {
        private const float Lift = 0.12f;
        private const float CentreWidth = 0.1f;
        private const float EdgeWidth = 0.07f;
        private const float MarkerHeight = 1.8f;
        private const float GhostAlpha = 0.45f;

        private static readonly Color MarkerColour = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color TentativeColour = new Color(1f, 1f, 1f, 0.5f);
        private static readonly LinePool Lines = new LinePool("EarthWright.PathPreview");
        private static readonly VertexDots Dots = new VertexDots();

        public static void Show(PathView view)
        {
            Material material = PreviewMaterial.Get();
            Lines.Begin(material);
            PathPlan plan = view.Plan != null && !view.Plan.TooShort ? view.Plan : null;
            float alpha = view.Ghostly ? GhostAlpha : 1f;
            if (plan != null)
                DrawPlan(plan, alpha);
            foreach (Vector3 marker in view.Markers)
                Lines.Segment(marker, marker + Vector3.up * MarkerHeight, MarkerColour, EdgeWidth);
            if (view.HasTentative)
                Lines.Segment(view.TentativeFrom + Vector3.up * Lift, view.TentativeTo + Vector3.up * Lift, TentativeColour, EdgeWidth);
            Lines.End();
            if (plan != null && PathSettings.ShowVertices.Value)
                Dots.Show(plan.Vertices, alpha, material);
            else
                Dots.Hide();
        }

        public static void Hide()
        {
            Lines.Hide();
            Dots.Hide();
        }

        private static void DrawPlan(PathPlan plan, float alpha)
        {
            int[] classes = SlopeColours.Classes(plan);
            if (classes.Length == 0)
                return;
            PathShape shape = plan.Draft.Shape;
            List<Vector3> left = Offset(plan.Line, shape.Left);
            List<Vector3> right = Offset(plan.Line, -shape.Right);
            DrawRuns(Offset(plan.Line, 0f), classes, alpha, CentreWidth);
            if (shape.Left > 0f)
                DrawRuns(left, classes, alpha, EdgeWidth);
            if (shape.Right > 0f)
                DrawRuns(right, classes, alpha, EdgeWidth);
            int last = left.Count - 1;
            Lines.Segment(left[0], right[0], SlopeColours.Colour(classes[0], alpha), EdgeWidth);
            Lines.Segment(left[last], right[last], SlopeColours.Colour(classes[classes.Length - 1], alpha), EdgeWidth);
        }

        /// <summary>One line per run of segments of the same slope class, since a line renderer holds few colour keys.</summary>
        private static void DrawRuns(List<Vector3> points, int[] classes, float alpha, float width)
        {
            int start = 0;
            for (int i = 1; i <= classes.Length; i++)
            {
                if (i < classes.Length && classes[i] == classes[start])
                    continue;
                Lines.Draw(points, start, i, SlopeColours.Colour(classes[start], alpha), width);
                start = i;
            }
        }

        /// <summary>The centre line moved sideways by <paramref name="offset"/> metres (positive to the left), lifted off the ground.</summary>
        private static List<Vector3> Offset(CentreLine line, float offset)
        {
            List<Vector3> points = new List<Vector3>(line.Count);
            for (int i = 0; i < line.Count; i++)
                points.Add(line.Points[i] + line.LeftNormal(i) * offset + Vector3.up * Lift);
            return points;
        }
    }
}
