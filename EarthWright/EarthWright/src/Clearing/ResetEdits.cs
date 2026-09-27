using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The reset strokes of the U key, Shift+U and 'ew reset': back to the world's generated height and ground texture,
    /// with full effect everywhere inside (no soft rim). They touch terrain only, never buildings, trees or rocks, and
    /// skip the ground under building pieces while "Reset Skips Ground Under Buildings" is on. Their sources
    /// (<see cref="Source"/> for the keys, <see cref="CommandSource"/> for 'ew reset', shown as "ew reset" by the undo
    /// history) are no entry, so they are never charged and start no cooldown; the Protection guards refuse wards.
    /// </summary>
    public static class ResetEdits
    {
        public const string Source = "reset";
        public const string CommandSource = "command:reset";

        /// <summary>A reset of the brush footprint (shape, size, rotation) centred on the given point.</summary>
        public static TerrainEdit Brush(Vector3 center)
        {
            ToolAction action = BrushState.Action;
            float radius = action != null && !action.Resizable ? action.BaseRadius : BrushState.Radius;
            BrushStroke stroke = Stroke(center, BrushState.Shape, radius);
            stroke.Radius2 = BrushState.Radius2;
            stroke.Rotation = BrushState.Rotation;
            TerrainEdit edit = Wrap(stroke);
            if (BrushState.GridMode)
                edit.Flags |= EditFlags.GridAligned;
            return edit;
        }

        /// <summary>A reset of a circle around a point (the player).</summary>
        public static TerrainEdit Around(Vector3 center, float radius, string source = Source) => Wrap(Stroke(center, BrushShape.Circle, radius), source);

        private static BrushStroke Stroke(Vector3 center, BrushShape shape, float radius)
        {
            return new BrushStroke
            {
                Center = center, Shape = shape, Radius = radius, Hardness = 1f, Height = HeightOp.Reset,
                Paint = PaintOp.Original, MaxStep = 0f, Seed = Random.Range(int.MinValue, int.MaxValue),
            };
        }

        private static TerrainEdit Wrap(BrushStroke stroke, string source = Source)
        {
            TerrainEdit edit = TerrainEdit.ForStroke(stroke, source);
            if (ClearingSettings.ResetSkipsPieces.Value)
                edit.Flags |= EditFlags.SkipUnderPieces;
            return edit;
        }
    }
}
