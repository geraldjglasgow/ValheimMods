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
            BrushStroke stroke = StrokeOf(action);
            ApplyPaint(stroke, action);
            ApplyOneShot(stroke);
            TerrainEdit edit = TerrainEdit.ForStroke(stroke, action.Id);
            if (BrushState.GridMode)
                edit.Flags |= EditFlags.GridAligned;
            if (action.PaintHeightCheck)
                edit.Flags |= EditFlags.PaintHeightCheck;
            if (action.AdminOnly)
                edit.Flags |= EditFlags.Privileged;
            return edit;
        }

        /// <summary>The footprint and height values of the stroke, from the brush state.</summary>
        private static BrushStroke StrokeOf(ToolAction action)
        {
            return new BrushStroke
            {
                Center = BrushState.Center,
                Shape = BrushState.Shape,
                Radius = action.Resizable ? BrushState.Radius : action.BaseRadius,
                Radius2 = BrushState.Radius2,
                Rotation = BrushState.Rotation,
                Hardness = BrushState.GridMode ? 1f : BrushState.Hardness,
                Height = action.Height,
                Style = BrushState.Style,
                Target = BrushState.TargetHeight,
                Amount = BrushState.Amount,
                MaxStep = BrushState.MaxStep,
                Strength = BrushState.Strength,
                Density = BrushState.Density,
                Seed = Random.Range(int.MinValue, int.MaxValue),
            };
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
