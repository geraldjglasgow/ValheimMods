using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Adds the plans of several heightmaps into one <see cref="EditEstimate"/>. A vertex or paint cell on an edge two
    /// heightmaps share is planned by both (each holds a copy) but counted once, keyed by its whole-metre world position.
    /// Volumes count every vertex as one square metre (times the heightmap scale squared).
    /// </summary>
    public sealed class EngineEstimateSum
    {
        private readonly HashSet<long> edgeVertices = new HashSet<long>();
        private readonly HashSet<long> edgeCells = new HashSet<long>();

        /// <summary>Starts a new estimate (forgets the edge points counted so far).</summary>
        public void Begin()
        {
            edgeVertices.Clear();
            edgeCells.Clear();
        }

        /// <summary>Adds one heightmap's plan to the estimate; the per-vertex list stops at <paramref name="changeLimit"/> entries.</summary>
        public void Add(HeightView view, ChangeBuffer changes, EditEstimate estimate, int changeLimit)
        {
            estimate.HitLimit |= changes.HitLimit;
            for (int n = 0; n < changes.HeightCount; n++)
            {
                HeightChange c = changes.Heights[n];
                if (Mathf.Abs(c.After - c.Before) >= EngineRecord.MinChange && FirstTime(view, c.Index, edgeVertices))
                    AddVertex(view, c, estimate, estimate.Changes.Count < changeLimit);
            }
            for (int n = 0; n < changes.PaintCount; n++)
            {
                if (FirstTime(view, changes.Paints[n].Index, edgeCells))
                    AddCell(changes.Paints[n], estimate);
            }
        }

        /// <summary>Counts a changed paint cell, and whether it becomes paved where it was not (paved = the blue channel).</summary>
        private static void AddCell(PaintChange c, EditEstimate estimate)
        {
            estimate.PaintCells++;
            if (c.NewColor.b > 0.5f && c.Before.b <= 0.5f)
                estimate.PavedCells++;
        }

        private static void AddVertex(HeightView view, HeightChange c, EditEstimate estimate, bool listed)
        {
            float area = view.Scale * view.Scale;
            estimate.Vertices++;
            estimate.Raised += Mathf.Max(0f, c.After - c.Before) * area;
            estimate.Lowered += Mathf.Max(0f, c.Before - c.After) * area;
            estimate.HitLimit |= c.Limited;
            if (!listed)
                return;
            int x = c.Index % view.Pitch;
            int y = c.Index / view.Pitch;
            estimate.Changes.Add(new VertexChange
            {
                X = view.VertexX(x), Z = view.VertexZ(y), Before = c.Before + view.OriginY, After = c.After + view.OriginY, Limited = c.Limited,
            });
        }

        /// <summary>False for an edge point another heightmap of this estimate already counted.</summary>
        private static bool FirstTime(HeightView view, int index, HashSet<long> seen)
        {
            int x = index % view.Pitch;
            int y = index / view.Pitch;
            if (!view.OnEdge(x, y))
                return true;
            long key = ((long)Mathf.RoundToInt(view.VertexX(x)) << 32) ^ (uint)Mathf.RoundToInt(view.VertexZ(y));
            return seen.Add(key);
        }
    }
}
