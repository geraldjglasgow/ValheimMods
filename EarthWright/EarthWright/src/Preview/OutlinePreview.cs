using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The footprint outline on the ground: the height footprint (outer and, for ring and frame, inner edge), and a
    /// second, thinner outline where the paint reaches when the paint footprint differs from it. A paint-only stroke
    /// shows its paint footprint as the main outline. Coloured normal, blocked or out of reach. Lines float a little
    /// above the ground so the terrain does not swallow them.
    /// </summary>
    internal static class OutlinePreview
    {
        private const float Lift = 0.1f;

        private static readonly LineStrip[] brushLines = { new LineStrip("EarthWright Outline"), new LineStrip("EarthWright Outline Inner") };
        private static readonly LineStrip[] paintLines = { new LineStrip("EarthWright Paint Outline"), new LineStrip("EarthWright Paint Outline Inner") };
        private static readonly GroundLoops paintLoops = new GroundLoops();
        private static readonly GroundLoops paintOnlyLoops = new GroundLoops();
        private static Vector3[] lifted = new Vector3[0];

        public static void Update()
        {
            BrushStroke stroke = PreviewFrame.Stroke;
            if (!PreviewFrame.BrushVisible || stroke == null || !PreviewSettings.ShowOutline.Value)
            {
                HideAll();
                return;
            }
            float width = PreviewSettings.OutlineWidth.Value;
            bool paintOnly = stroke.Height == HeightOp.None;
            GroundLoops source = paintOnly ? paintOnlyLoops : GroundLoops.Brush;
            source.Get(paintOnly ? PreviewFrame.PaintSpec : PreviewFrame.HeightSpec, out List<Vector3[]> loops);
            Draw(brushLines, loops, PreviewFrame.ToneColour(PreviewSettings.OutlineColour.Value), width);
            if (paintOnly || !HasOwnPaintOutline(stroke))
            {
                Hide(paintLines);
                return;
            }
            paintLoops.Get(PreviewFrame.PaintSpec, out List<Vector3[]> paint);
            Draw(paintLines, paint, PreviewFrame.ToneColour(PreviewSettings.PaintOutlineColour.Value), width * 0.75f);
        }

        /// <summary>The stroke paints, and its paint footprint is visibly larger or smaller than the height footprint.</summary>
        private static bool HasOwnPaintOutline(BrushStroke stroke)
        {
            return stroke.Paint != PaintOp.None && Mathf.Abs(PreviewFrame.PaintSpec.Print.Outer - PreviewFrame.HeightSpec.Print.Outer) > 0.05f;
        }

        private static void Draw(LineStrip[] lines, List<Vector3[]> loops, Color colour, float width)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (i >= loops.Count)
                {
                    lines[i].Hide();
                    continue;
                }
                Vector3[] points = Lifted(loops[i]);
                lines[i].Show(points, loops[i].Length, true, colour, width);
            }
        }

        private static Vector3[] Lifted(Vector3[] points)
        {
            if (lifted.Length < points.Length)
                lifted = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                lifted[i] = points[i] + Vector3.up * Lift;
            return lifted;
        }

        public static void HideAll()
        {
            Hide(brushLines);
            Hide(paintLines);
        }

        private static void Hide(LineStrip[] lines)
        {
            foreach (LineStrip line in lines)
                line.Hide();
        }
    }
}
