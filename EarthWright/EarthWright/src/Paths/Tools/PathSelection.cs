using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// What the ramp and road tools read from the rest of the mod: which entry is selected, where the cursor is, the
    /// height a clicked point gets, the width from the brush size and the surface paint from the player's paint choice.
    /// </summary>
    public static class PathSelection
    {
        public const string RampKey = "ramp";
        public const string RoadKey = "road";

        /// <summary>The selected entry's action while a terrain tool is in use with its menu closed and EarthWright on; else null.</summary>
        public static ToolAction Current => GeneralSettings.Active ? ActionCatalog.Current : null;

        /// <summary>The special key of the entry selected in the held terrain tool, build menu open or not; null when there is none.</summary>
        public static string SelectedSpecial
        {
            get
            {
                Player player = LocalTool.Player;
                if (!GeneralSettings.Active || player == null || player.IsDead() || !player.InPlaceMode())
                    return null;
                if (!LocalTool.IsToolName(LocalTool.RightItemName))
                    return null;
                return ActionCatalog.For(player.GetSelectedPiece())?.Special;
            }
        }

        public static bool IsRamp(ToolAction action) => action != null && action.Special == RampKey;

        public static bool IsRoad(ToolAction action) => action != null && action.Special == RoadKey;

        /// <summary>The local player takes keyboard input now (no chat, console, inventory, menu, map or text field).</summary>
        public static bool InputAllowed
        {
            get
            {
                Player player = LocalTool.Player;
                return player != null && Hud.instance != null && player.TakeInput() && !Keys.TextInputActive;
            }
        }

        /// <summary>
        /// Where the player aims: the brush centre while the Brush module follows this entry (it finds the ground behind
        /// rocks and snaps to whole metres in grid mode), otherwise the placement ghost. False while nothing is aimed at.
        /// </summary>
        public static bool TryCursor(out Vector3 position)
        {
            position = Vector3.zero;
            if (BrushState.Active && BrushState.Action != null && BrushState.Action == ActionCatalog.Current)
            {
                position = BrushState.Center;
                return BrushState.HasAim;
            }
            GameObject ghost = LocalTool.Ghost;
            if (ghost == null)
                return false;
            position = ghost.transform.position;
            return true;
        }

        public static Vector3 CursorOr(Vector3 fallback) => TryCursor(out Vector3 position) ? position : fallback;

        /// <summary>
        /// The point with the height it gets: the locked, exact or copied floor target height when the player set one,
        /// otherwise the ground's height there.
        /// </summary>
        public static Vector3 WithHeight(Vector3 position)
        {
            position.y = UsesSetHeight ? BrushState.TargetHeight : TerrainRead.GroundHeight(position, position.y);
            return position;
        }

        /// <summary>The player set a height (locked, exact or copied from a floor) that clicked points take instead of the ground's.</summary>
        public static bool UsesSetHeight
        {
            get
            {
                TargetSource source = BrushState.TargetSource;
                return source == TargetSource.Locked || source == TargetSource.Exact || source == TargetSource.Floor;
            }
        }

        /// <summary>The width from the brush size: twice the radius, within the width caps.</summary>
        public static float BrushWidth() => PathSettings.ClampWidth(2f * BrushState.Radius);

        /// <summary>The surface paint: the player's paint choice, or the given default while the choice is the entry's own.</summary>
        public static PaintOp Paint(PathPaint entryPaint)
        {
            if (BrushState.PaintOverride != PaintOp.None)
                return BrushState.PaintOverride;
            return BrushState.KeepPaint ? PaintOp.None : ToPaintOp(entryPaint);
        }

        public static PaintOp ToPaintOp(PathPaint paint)
        {
            switch (paint)
            {
                case PathPaint.Dirt: return PaintOp.Dirt;
                case PathPaint.Paved: return PaintOp.Paved;
                case PathPaint.Cultivated: return PaintOp.Cultivated;
                case PathPaint.Grass: return PaintOp.Grass;
                default: return PaintOp.None;
            }
        }
    }
}
