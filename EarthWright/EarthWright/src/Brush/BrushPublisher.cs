using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Writes the selected entry's values into <see cref="BrushState"/> every frame, clamped to the current limits
    /// (which can change while a value stays remembered: another tool level, a server setting, grid mode). Everything
    /// that reads the brush (edit factory, preview, costs, paths) sees only these published values. Local only.
    /// </summary>
    public static class BrushPublisher
    {
        /// <summary>The footprint rotation the player set; grid mode publishes 0 without forgetting it.</summary>
        public static float Rotation;

        public static void Publish(ToolAction action, BrushValues values)
        {
            BrushShape shape = EntryKinds.IsPath(action) ? BrushShape.Circle : ShapeCycle.Effective(values);
            BrushLimits.RadiusRange(values.Defaults, out _, out float maxRadius);
            BrushState.Shape = shape;
            BrushState.Radius = BrushLimits.Radius(values, values.Radius);
            BrushState.Radius2 = BrushLimits.Radius2(shape, BrushState.Radius, values.Radius2, maxRadius);
            BrushState.Rotation = BrushState.GridMode || !ValueSelector.Turnable(shape) ? 0f : Rotation;
            BrushState.Hardness = Mathf.Clamp01(values.Hardness);
            BrushState.Amount = BrushLimits.Amount(values.Amount);
            BrushState.MaxStep = BrushLimits.MaxStep(values.MaxStep);
            BrushState.Strength = Mathf.Clamp01(values.Strength);
            BrushState.Density = Mathf.Clamp01(BrushState.Density);
            BrushState.Style = StyleCycle.Effective(values);
            PaintCycle.Publish(action, values);
            ExternalEdits.RecordBrush();
        }
    }
}
