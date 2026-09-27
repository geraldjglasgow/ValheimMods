using System;

namespace EarthWright.Terrain
{
    /// <summary>The footprint of a brush stroke.</summary>
    public enum BrushShape : byte
    {
        /// <summary>Radius around the centre.</summary>
        Circle = 0,
        /// <summary>Radius is the half side; turned by the stroke's rotation.</summary>
        Square = 1,
        /// <summary>Radius is the half width, Radius2 the half depth; turned by the rotation.</summary>
        Rectangle = 2,
        /// <summary>Between Radius2 (inner) and Radius (outer) around the centre.</summary>
        Ring = 3,
        /// <summary>A square outline: between Radius - Radius2 and Radius from the centre, turned by the rotation.</summary>
        Frame = 4,
    }

    /// <summary>What a stroke does to the height of each vertex it covers.</summary>
    public enum HeightOp : byte
    {
        /// <summary>Height untouched (paint-only strokes).</summary>
        None = 0,
        /// <summary>Toward <see cref="BrushStroke.Target"/>, in the stroke's <see cref="LevelStyle"/>.</summary>
        Level = 1,
        /// <summary>Up by <see cref="BrushStroke.Amount"/> metres (scaled by the edge weight).</summary>
        Raise = 2,
        /// <summary>Down by <see cref="BrushStroke.Amount"/> metres (scaled by the edge weight).</summary>
        Lower = 3,
        /// <summary>Evens out ridges: each vertex moves toward the average of its neighbours by <see cref="BrushStroke.Strength"/>.</summary>
        Smooth = 4,
        /// <summary>Back to the world's generated height (the edit is forgotten).</summary>
        Reset = 5,
        /// <summary>Only raises: vertices below Target are lifted to it, the rest untouched.</summary>
        SetMin = 6,
        /// <summary>Only lowers: vertices above Target are cut down to it, the rest untouched.</summary>
        SetMax = 7,
        /// <summary>Height becomes the generated height plus <see cref="BrushStroke.Amount"/>.</summary>
        Offset = 8,
        /// <summary>Digs down to the dig limit (removes the surface entirely).</summary>
        RemoveSurface = 9,
    }

    /// <summary>How <see cref="HeightOp.Level"/> approaches its target.</summary>
    public enum LevelStyle : byte
    {
        /// <summary>Eases toward the target by the edge weight, at most MaxStep per click (vanilla-like).</summary>
        Ease = 0,
        /// <summary>Moves straight toward the target, at most MaxStep per click, full strength inside the hard core.</summary>
        Step = 1,
        /// <summary>Sets every covered vertex to the target at once (a plateau; soft edges still blend).</summary>
        Instant = 2,
    }

    /// <summary>What a stroke paints on the ground texture.</summary>
    public enum PaintOp : byte
    {
        None = 0,
        Dirt = 1,
        Cultivated = 2,
        Paved = 3,
        /// <summary>The game's own "grass" paint (clears dirt, cultivation and paving; what the cultivator's replant does).</summary>
        Grass = 4,
        /// <summary>Removes grass and ground clutter (vegetation alpha to 0).</summary>
        ClearVegetation = 5,
        /// <summary>Sets the vegetation alpha to <see cref="BrushStroke.Density"/> (grass density brush).</summary>
        Vegetation = 6,
        DeepSnow = 7,
        /// <summary>Forgets painted texture: the world's generated ground shows again.</summary>
        Original = 8,
    }

    /// <summary>Options carried by every edit.</summary>
    [Flags]
    public enum EditFlags : ushort
    {
        None = 0,
        /// <summary>The edit asks for admin rights; it travels through the server, which checks the sender.</summary>
        Privileged = 1,
        /// <summary>Height limits do not apply (only honoured on a privileged edit the server approved).</summary>
        IgnoreLimits = 2,
        /// <summary>Snap the footprint to whole world metres; every covered vertex gets the full effect.</summary>
        GridAligned = 4,
        /// <summary>Skip vertices that have a building piece standing over them.</summary>
        SkipUnderPieces = 8,
        /// <summary>Only affect vertices whose current height lies within [BandMin, BandMax].</summary>
        HeightBand = 16,
        /// <summary>Only paint where the ground is at or below the stroke's reference height (vanilla's paint height check).</summary>
        PaintHeightCheck = 32,
        /// <summary>Recorded by the undo history as a restore; not itself recorded again.</summary>
        IsRestore = 64,
        /// <summary>Made by a click through the game's own placement (which charged stamina, durability and the entry's resources).</summary>
        FromPlacement = 128,
    }

    /// <summary>The two forms of edit.</summary>
    public enum EditKind : byte
    {
        /// <summary>A parametric brush stroke, evaluated by the owner against its own current terrain.</summary>
        Stroke = 1,
        /// <summary>Explicit per-vertex values computed by the sender (ramps, roads, undo).</summary>
        Vertices = 2,
    }

    /// <summary>What the entries of a <see cref="VertexSet"/> mean.</summary>
    public enum VertexMode : byte
    {
        /// <summary>World vertex coordinates with a target height and blend weight; optional paint.</summary>
        Targets = 1,
        /// <summary>Raw TerrainComp values by array index, for one compiler (undo and redo).</summary>
        Restore = 2,
    }
}
