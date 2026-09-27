using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Fills a <see cref="HeightView"/> from the game's objects on this machine: from a terrain compiler (its edit arrays
    /// are referenced, not copied, so the owner writes straight into them) or from a heightmap nobody has edited yet (its
    /// shown heights and paint texture). Views are reused; nothing here allocates once the buffers exist.
    /// </summary>
    public static class EngineViews
    {
        private static readonly Func<float, float, float> outside = OutsideHeight;
        private static readonly Func<float, float, float, bool> covered = EnginePieceProbe.Covered;

        /// <summary>A view of a compiler this machine holds (owner or replicated copy); false when it is not usable.</summary>
        public static bool ForComp(TerrainComp comp, HeightView view)
        {
            if (comp == null || !comp.m_initialized || !Common(comp.m_hmap, view))
                return false;
            int count = view.Count;
            if (comp.m_levelDelta.Length != count || comp.m_smoothDelta.Length != count || comp.m_modifiedHeight.Length != count
                || comp.m_paintMask.Length != count || comp.m_modifiedPaint.Length != count)
                return false;
            view.Base = EngineBaseHeights.For(comp.m_hmap, comp, ref view.OwnBase);
            view.Level = comp.m_levelDelta;
            view.Smooth = comp.m_smoothDelta;
            view.Modified = comp.m_modifiedHeight;
            view.Paint = comp.m_paintMask;
            view.PaintModified = comp.m_modifiedPaint;
            return true;
        }

        /// <summary>A view of a heightmap: through its compiler when it has one, else its own shown heights and paint.</summary>
        public static bool ForMap(Heightmap map, HeightView view)
        {
            if (map == null)
                return false;
            TerrainComp comp = TerrainComp.FindTerrainCompiler(map.transform.position);
            if (comp != null && comp.m_initialized && comp.m_hmap == map)
                return ForComp(comp, view);
            if (!Common(map, view))
                return false;
            view.Base = EngineBaseHeights.CopyShown(map, ref view.OwnBase);
            view.Level = null;
            view.Smooth = null;
            view.Modified = null;
            view.Paint = null;
            view.PaintModified = null;
            return true;
        }

        private static bool Common(Heightmap map, HeightView view)
        {
            if (map == null || map.m_buildData == null || map.m_heights.Count != (map.m_width + 1) * (map.m_width + 1))
                return false;
            Vector3 origin = map.transform.position;
            view.Width = map.m_width;
            view.Scale = map.m_scale;
            view.OriginX = origin.x;
            view.OriginY = origin.y;
            view.OriginZ = origin.z;
            view.BaseMask = map.m_buildData.m_baseMask;
            view.PaintTexture = map.m_paintMask;
            view.Absolute = HeightLimits.Absolute;
            view.OutsideHeight = outside;
            view.UnderPiece = null;
            return true;
        }

        /// <summary>
        /// The building-piece probe for an edit that skips ground under buildings, or null when it does not (or no piece
        /// stands anywhere in its area, which saves a physics query per point). Set it on each view after filling it.
        /// </summary>
        public static Func<float, float, float, bool> PieceProbeFor(TerrainEdit edit)
        {
            if (edit == null || !edit.Has(EditFlags.SkipUnderPieces))
                return null;
            edit.GetArea(out Vector3 center, out float radius);
            return EnginePieceProbe.AnyNear(center, radius) ? covered : null;
        }

        /// <summary>The shown world height at a position on whatever heightmap holds it, NaN when none is loaded.</summary>
        private static float OutsideHeight(float x, float z)
        {
            return Heightmap.GetHeight(new Vector3(x, 0f, z), out float height) ? height : float.NaN;
        }
    }
}
