using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Hands the brush's lines to the HUD block ("brush" and "target" in <see cref="HudText"/>), which the Preview module
    /// draws. The lines are built again only when a value they show changed (<see cref="BrushHudKey"/>), and at least
    /// twice a second for what the key does not hold (tool level unlocks). The keys are listed once, in the selected
    /// entry's description (the Menu module). Local display only.
    /// </summary>
    public static class BrushHud
    {
        public const int BrushOrder = 10;
        public const int TargetOrder = 20;

        private const float RefreshSeconds = 0.5f;

        private static BrushHudKey shownKey;
        private static float builtAt = -10f;

        public static void Show(ToolAction action, BrushValues values)
        {
            BrushHudKey key = BrushHudKey.Now(action, values);
            if (key.Same(shownKey) && Time.time - builtAt < RefreshSeconds)
                return;
            shownKey = key;
            builtAt = Time.time;
            HudText.Set("brush", BrushHudText.BrushLine(action, values), BrushOrder);
            HudText.Set("target", BrushHudText.TargetLine(action, values), TargetOrder);
        }

        public static void Hide()
        {
            HudText.Clear("brush");
            HudText.Clear("target");
            shownKey = default;
        }
    }

    /// <summary>Everything the brush's HUD lines show, compared each frame without building any text.</summary>
    internal struct BrushHudKey
    {
        private ToolAction action;
        private BrushValues values;
        private Vector4 sizes;
        private Vector4 amounts;
        private float target;
        private BrushShape shape;
        private LevelStyle style;
        private BrushValue selected;
        private TargetSource source;
        private TargetSource mode;
        private PaintChoice paint;
        private int flags;

        public static BrushHudKey Now(ToolAction action, BrushValues values)
        {
            return new BrushHudKey
            {
                action = action, values = values,
                sizes = new Vector4(BrushState.Radius, BrushState.Radius2, BrushState.Rotation, BrushState.Hardness),
                amounts = new Vector4(BrushState.Amount, BrushState.MaxStep, BrushState.Strength, BrushState.Density),
                target = BrushState.TargetHeight, shape = BrushState.Shape, style = BrushState.Style,
                selected = BrushState.Selected, source = BrushState.TargetSource, mode = BrushState.TargetMode,
                paint = PaintCycle.Effective(action, values),
                flags = (BrushState.GridMode ? 1 : 0) | (BrushState.AimAtEdge ? 2 : 0) | (TargetState.IsFixed ? 4 : 0),
            };
        }

        public bool Same(BrushHudKey other)
        {
            return action == other.action && values == other.values && sizes == other.sizes && amounts == other.amounts
                && target == other.target && shape == other.shape && style == other.style && selected == other.selected
                && source == other.source && mode == other.mode && paint == other.paint && flags == other.flags;
        }
    }
}
