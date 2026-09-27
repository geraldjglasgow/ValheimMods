using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// Builds the combined mesh of point markers: a small flat diamond per changed vertex, coloured by what happens to
    /// it (rises, sinks, held back by a height limit), or per painted cell for paint-only strokes. A marker sits at the
    /// higher of the two heights so it is never buried: above the ground for a raise, on the ground for a cut.
    /// </summary>
    internal static class PointMarkers
    {
        private const float Lift = 0.06f;

        /// <summary>Markers for the vertices of an estimate, at most <paramref name="limit"/>.</summary>
        public static void FromChanges(MeshData data, List<VertexChange> changes, Vector3 origin, int limit)
        {
            data.Clear(origin);
            float half = PreviewSettings.PointSize.Value * 0.5f;
            Color raise = PreviewSettings.RaiseColour.Value;
            Color lower = PreviewSettings.LowerColour.Value;
            Color limited = PreviewSettings.LimitedColour.Value;
            int count = Mathf.Min(changes.Count, limit);
            for (int i = 0; i < count; i++)
            {
                VertexChange change = changes[i];
                Color colour = change.Limited ? limited : change.After >= change.Before ? raise : lower;
                Vector3 at = new Vector3(change.X, Mathf.Max(change.Before, change.After) + Lift, change.Z);
                Diamond(data, at, half, colour);
            }
        }

        /// <summary>
        /// Markers for every paint cell the engine's paint footprint covers. A paint cell belongs to a vertex and covers
        /// the square metre toward +X and +Z, so its centre is half a metre off the vertex on both axes (the engine's
        /// HeightView convention). Cells that already carry the paint are marked too: the estimate only counts them.
        /// </summary>
        public static void FromPaint(MeshData data, FootprintSpec spec, int limit)
        {
            data.Clear(spec.Center);
            spec.Print.Extent(out float halfX, out float halfZ);
            int x0 = Mathf.CeilToInt(spec.Print.CenterX - halfX - 0.5f), x1 = Mathf.FloorToInt(spec.Print.CenterX + halfX - 0.5f);
            int z0 = Mathf.CeilToInt(spec.Print.CenterZ - halfZ - 0.5f), z1 = Mathf.FloorToInt(spec.Print.CenterZ + halfZ - 0.5f);
            int added = 0;
            for (int x = x0; x <= x1 && added < limit; x++)
            {
                for (int z = z0; z <= z1 && added < limit; z++)
                {
                    if (spec.Covers(x + 0.5f, z + 0.5f))
                    {
                        Cell(data, spec, x + 0.5f, z + 0.5f);
                        added++;
                    }
                }
            }
        }

        private static void Cell(MeshData data, FootprintSpec spec, float x, float z)
        {
            Vector3 at = new Vector3(x, 0f, z);
            at.y = GroundSampler.Height(at, spec.CenterY) + Lift;
            Diamond(data, at, PreviewSettings.PointSize.Value * 0.5f, PreviewSettings.PaintCellColour.Value);
        }

        private static void Diamond(MeshData data, Vector3 at, float half, Color colour)
        {
            data.Quad(at + new Vector3(0f, 0f, half), at + new Vector3(half, 0f, 0f), at + new Vector3(0f, 0f, -half), at + new Vector3(-half, 0f, 0f), colour);
        }
    }
}
