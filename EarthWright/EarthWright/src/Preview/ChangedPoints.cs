using System.Globalization;
using System.Text;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The exact preview of what a click would change: asks for the estimate of the current edit (the same planners the
    /// owner runs, through <see cref="LiveEstimate"/>, which the cost line shares; at most five times a second, twice for
    /// very large brushes, and planned again only when the brush moved or changed or a second has passed so edits by
    /// anyone show up), marks the changed vertices (up to the point limit) in one combined mesh, and puts
    /// "N points, +X m³ / −Y m³" into the HUD. Nothing is redrawn while the estimate and the marker settings stay.
    /// </summary>
    internal static class ChangedPoints
    {
        public const string HudKey = "preview";

        private const float MinInterval = 0.2f;

        /// <summary>Brushes wider than this (metres of reach) plan tens of thousands of points per estimate: ask twice a second.</summary>
        private const float LargeReach = 30f;
        private const float LargeInterval = 0.5f;

        private static readonly MeshLayer layer = new MeshLayer("EarthWright Points");
        private static readonly MeshData data = new MeshData();
        private static float nextCheck;
        private static EditEstimate shown;
        private static Vector3 shownLook;

        public static void Update()
        {
            TerrainEdit edit = PreviewFrame.Edit;
            if (!PreviewFrame.BrushVisible || edit == null)
            {
                Clear();
                return;
            }
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + (edit.Stroke.Reach > LargeReach ? LargeInterval : MinInterval);
            bool markers = PreviewSettings.ShowPoints.Value;
            int limit = markers ? PreviewSettings.PointLimit.Value : 0;
            LiveEstimate.PreferredChanges = limit;
            EditEstimate estimate = Safe.Call("EarthWright preview estimate", (e, l) => LiveEstimate.For(e, l), edit, limit, null);
            Vector3 look = new Vector3(markers ? 1f : 0f, PreviewSettings.PointLimit.Value, PreviewSettings.PointSize.Value);
            if (estimate != null && estimate == shown && look == shownLook)
                return;
            shown = estimate;
            shownLook = look;
            Publish(estimate);
            Draw(edit.Stroke, estimate, markers);
        }

        private static void Draw(BrushStroke stroke, EditEstimate estimate, bool markers)
        {
            int limit = PreviewSettings.PointLimit.Value;
            if (!markers)
                layer.Hide();
            else if (stroke.Height == HeightOp.None && stroke.Paint != PaintOp.None)
                PointMarkers.FromPaint(data, PreviewFrame.PaintSpec, limit);
            else if (estimate != null)
                PointMarkers.FromChanges(data, estimate.Changes, stroke.Center, limit);
            else
                data.Clear(stroke.Center);
            if (markers)
                layer.Show(data);
        }

        /// <summary>The HUD line with the number of points and the volumes moved.</summary>
        private static void Publish(EditEstimate estimate)
        {
            if (estimate == null || (estimate.Vertices == 0 && estimate.PaintCells == 0))
            {
                HudText.Clear(HudKey);
                return;
            }
            StringBuilder text = new StringBuilder();
            if (estimate.Vertices > 0)
                text.Append(string.Format(CultureInfo.InvariantCulture, "{0} $ew_preview_points, +{1:0.0} m³ / −{2:0.0} m³", estimate.Vertices, estimate.Raised, estimate.Lowered));
            if (estimate.PaintCells > 0)
                text.Append(text.Length > 0 ? " · " : "").Append(estimate.PaintCells).Append(" $ew_preview_cells");
            if (estimate.HitLimit)
                text.Append(" · <color=#ff5577>$ew_preview_limit</color>");
            HudText.Set(HudKey, text.ToString(), 50);
        }

        public static void Clear()
        {
            layer.Hide();
            HudText.Clear(HudKey);
            shown = null;
            LiveEstimate.PreferredChanges = 0;
        }
    }
}
