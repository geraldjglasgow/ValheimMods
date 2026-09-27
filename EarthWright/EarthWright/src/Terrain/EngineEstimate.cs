using System;
using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Sender side, read-only: runs the owner's planners over every heightmap the edit touches on this machine (through
    /// its compiler where it has one, else its own heights and paint) and sums what would change
    /// (<see cref="EngineEstimateSum"/>, which counts shared edge points once). A privileged edit (ignoring the limits,
    /// past the size caps) is estimated that way only for a local admin, as the server would approve it. Gentle slopes
    /// are not part of the estimate.
    /// </summary>
    public static class EngineEstimate
    {
        private static List<Heightmap> maps;
        private static readonly HeightView view = new HeightView();
        private static readonly ChangeBuffer changes = new ChangeBuffer();
        private static readonly LimitContext limits = new LimitContext();
        private static readonly EngineEstimateSum sum = new EngineEstimateSum();

        public static EditEstimate Run(TerrainEdit edit, bool withChanges)
        {
            EditEstimate estimate = new EditEstimate();
            if (edit == null || !FindMaps(edit))
                return estimate;
            bool admin = edit.Has(EditFlags.Privileged) && Side.LocalIsAdmin;
            limits.Reset(admin && edit.Has(EditFlags.IgnoreLimits), admin);
            Func<float, float, float, bool> probe = EngineViews.PieceProbeFor(edit);
            sum.Begin();
            foreach (Heightmap map in maps)
            {
                if (!EngineViews.ForMap(map, view))
                    continue;
                view.UnderPiece = probe;
                EnginePlanner.Plan(view, edit, limits, changes);
                sum.Add(view, changes, estimate, withChanges);
            }
            return estimate;
        }

        /// <summary>The loaded heightmaps the edit may touch (an undo restore addresses exactly one).</summary>
        private static bool FindMaps(TerrainEdit edit)
        {
            maps = maps ?? new List<Heightmap>();
            maps.Clear();
            if (edit.Kind == EditKind.Vertices && edit.Vertices != null && edit.Vertices.Mode == VertexMode.Restore)
            {
                Heightmap map = Heightmap.FindHeightmap(edit.Vertices.CompPosition);
                if (map != null)
                    maps.Add(map);
            }
            else if (edit.Kind == EditKind.Stroke ? edit.Stroke != null : edit.Vertices != null)
            {
                edit.GetArea(out Vector3 center, out float radius);
                Heightmap.FindHeightmap(center, radius, maps);
            }
            return maps.Count > 0;
        }
    }
}
