using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>Where the level target height came from, shown in the HUD.</summary>
    public enum TargetSource : byte
    {
        /// <summary>The ground under the player's feet (the game's own default for level ground).</summary>
        Feet = 0,
        /// <summary>The ground under the crosshair.</summary>
        Aimed = 1,
        /// <summary>A height locked with the lock key.</summary>
        Locked = 2,
        /// <summary>An exact world height set with the height keys.</summary>
        Exact = 3,
        /// <summary>The height of earlier edits next to the cursor ("continue the flat").</summary>
        Continued = 4,
        /// <summary>The height of the floor piece that was aimed at.</summary>
        Floor = 5,
    }

    /// <summary>Which brush value the wheel and the increase/decrease keys change (picked with the select key).</summary>
    public enum BrushValue : byte
    {
        Radius = 0,
        /// <summary>Raise/lower amount; for level entries the max step, for smooth the strength, for vegetation paint the density.</summary>
        Amount = 1,
        Hardness = 2,
        Rotation = 3,
        /// <summary>Radius2: rectangle half depth, ring inner radius, frame band width.</summary>
        Depth = 4,
        /// <summary>The exact target height (changing it switches the target to Exact).</summary>
        Target = 5,
    }

    /// <summary>
    /// The local player's brush: the values every stroke is built from. Written only by the Brush module (input and
    /// the ghost patch), read by everyone else (preview, costs, paths, the edit factory). Client-side state, never synced.
    /// </summary>
    public static class BrushState
    {
        /// <summary>A terrain tool is out with a terrain entry selected and EarthWright is on for this player.</summary>
        public static bool Active;

        /// <summary>The selected entry's action while <see cref="Active"/>.</summary>
        public static ToolAction Action;

        public static BrushShape Shape = BrushShape.Circle;

        /// <summary>Circle radius or square half side, metres.</summary>
        public static float Radius = 3f;

        /// <summary>Rectangle half depth, ring inner radius or frame band width.</summary>
        public static float Radius2 = 1f;

        /// <summary>Degrees about the vertical axis (square, rectangle, frame).</summary>
        public static float Rotation;

        /// <summary>0 soft .. 1 hard.</summary>
        public static float Hardness = 0.3f;

        /// <summary>Raise or lower amount, metres.</summary>
        public static float Amount = 1f;

        public static LevelStyle Style = LevelStyle.Ease;

        /// <summary>Largest change per click for Ease and Step levelling, metres.</summary>
        public static float MaxStep = 1f;

        /// <summary>Smoothing strength 0..1.</summary>
        public static float Strength = 0.5f;

        /// <summary>A paint chosen by the player that replaces the entry's own paint; None keeps the entry's paint.</summary>
        public static PaintOp PaintOverride = PaintOp.None;

        /// <summary>The player chose "keep the existing paint" (height only).</summary>
        public static bool KeepPaint;

        /// <summary>Vegetation density for the grass brush, 0..1.</summary>
        public static float Density = 1f;

        /// <summary>Grid mode: footprint snapped to whole metres, full effect on every covered vertex.</summary>
        public static bool GridMode;

        /// <summary>The centre of the footprint in the world (after snapping and aim-at-edge).</summary>
        public static Vector3 Center;

        /// <summary>The absolute target height for level-like entries.</summary>
        public static float TargetHeight;

        public static TargetSource TargetSource = TargetSource.Feet;

        /// <summary>One-shot overrides for the next click only (the hard-level key); the edit factory applies and clears them.</summary>
        public static HeightOp? NextHeight;
        public static LevelStyle? NextStyle;
        public static float? NextHardness;

        /// <summary>The value the wheel and the increase/decrease keys change now.</summary>
        public static BrushValue Selected = BrushValue.Radius;

        /// <summary>The crosshair marks the near edge of the footprint; <see cref="Center"/> is moved forward by the radius.</summary>
        public static bool AimAtEdge;

        /// <summary>The ground under the crosshair was found this frame (<see cref="AimPoint"/> and <see cref="Center"/> are current).</summary>
        public static bool HasAim;

        /// <summary>The ground point under the crosshair (terrain only, before snapping and aim-at-edge).</summary>
        public static Vector3 AimPoint;

        /// <summary>The target mode the player chose; <see cref="TargetSource"/> is where this frame's height actually came from.</summary>
        public static TargetSource TargetMode = TargetSource.Feet;
    }
}
