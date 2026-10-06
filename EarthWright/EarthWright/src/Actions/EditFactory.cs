using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Actions
{
    /// <summary>
    /// Builds the brush stroke of a click from the entry's action and the local brush state. The Brush module keeps
    /// <see cref="BrushState"/> current every frame (ghost position, target height, size), so this only combines them.
    /// </summary>
    public static class EditFactory
    {
        /// <summary>The edit a click with this action would send now; also used by the preview and the costs.</summary>
        public static TerrainEdit Build(ToolAction action)
        {
            TerrainEdit edit = TerrainEdit.ForStroke(new BrushStroke(), action.Id);
            Fill(edit, action);
            return edit;
        }

        /// <summary>Writes what <see cref="Build"/> would return now into a kept edit (the preview's, every frame).</summary>
        public static void Fill(TerrainEdit edit, ToolAction action)
        {
            edit.ResetStroke(action.Id);
            BrushStroke stroke = edit.Stroke;
            FillStroke(stroke, action);
            ApplyPaint(stroke, action);
            ApplyOneShot(stroke);
            if (BrushState.GridMode)
                edit.Flags |= EditFlags.GridAligned;
            if (action.PaintHeightCheck)
                edit.Flags |= EditFlags.PaintHeightCheck;
            if (action.AdminOnly)
                edit.Flags |= EditFlags.Privileged;
        }

        /// <summary>The footprint and height values of the stroke, from the brush state (every field, the stroke may be reused).</summary>
        private static void FillStroke(BrushStroke s, ToolAction action)
        {
            s.Center = BrushState.Center;
            s.Shape = BrushState.Shape;
            s.Radius = action.Resizable ? BrushState.Radius : action.BaseRadius;
            s.Radius2 = BrushState.Radius2;
            s.Rotation = BrushState.Rotation;
            s.Hardness = BrushState.GridMode ? 1f : BrushState.Hardness;
            s.Height = action.Height;
            s.Style = BrushState.Style;
            s.Target = BrushState.TargetHeight;
            s.Amount = BrushState.Amount;
            s.MaxStep = BrushState.MaxStep;
            s.Strength = BrushState.Strength;
            s.Density = BrushState.Density;
            s.PaintStrength = 1f;
            s.BandMin = 0f;
            s.BandMax = 0f;
            s.RandomShare = 1f;
            s.Seed = Random.Range(int.MinValue, int.MaxValue);
        }

        /// <summary>The hard-level key's overrides; they apply to one built edit and are cleared by <see cref="ClearOneShot"/>.</summary>
        private static void ApplyOneShot(BrushStroke stroke)
        {
            if (BrushState.NextHeight.HasValue)
                stroke.Height = BrushState.NextHeight.Value;
            if (BrushState.NextStyle.HasValue)
                stroke.Style = BrushState.NextStyle.Value;
            if (BrushState.NextHardness.HasValue)
                stroke.Hardness = BrushState.NextHardness.Value;
        }

        /// <summary>Called once a click's edit has been sent: the one-shot overrides are used up.</summary>
        public static void ClearOneShot()
        {
            BrushState.NextHeight = null;
            BrushState.NextStyle = null;
            BrushState.NextHardness = null;
        }

        private static void ApplyPaint(BrushStroke stroke, ToolAction action)
        {
            stroke.Paint = BrushState.KeepPaint && action.Height != HeightOp.None ? PaintOp.None : action.Paint;
            if (BrushState.PaintOverride != PaintOp.None)
                stroke.Paint = BrushState.PaintOverride;
            stroke.PaintRadius = stroke.Radius * (action.PaintRatio > 0f ? action.PaintRatio : 1f);
        }
    }
}
