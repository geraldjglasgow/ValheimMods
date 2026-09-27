using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>What the "amount" slot of the selector means for an entry.</summary>
    public enum AmountKind
    {
        None = 0,
        /// <summary>Raise or lower by metres.</summary>
        Amount,
        /// <summary>Level: largest change per click (Ease and Step styles).</summary>
        MaxStep,
        /// <summary>Smooth: strength 0..1.</summary>
        Strength,
        /// <summary>Vegetation paint: grass density 0..1.</summary>
        Density,
    }

    /// <summary>
    /// The value selector: which brush values apply to the selected entry and shape, and cycling through them with the
    /// select key. Values that do not apply (rotation of a circle, depth of a square, the target of a paint entry) are
    /// skipped. Ramps and roads only offer the radius, their half width.
    /// </summary>
    public static class ValueSelector
    {
        private static readonly BrushValue[] Order =
        {
            BrushValue.Radius, BrushValue.Amount, BrushValue.Hardness, BrushValue.Rotation, BrushValue.Depth, BrushValue.Target,
        };

        public static bool Applies(BrushValue value, ToolAction action, BrushValues values)
        {
            if (EntryKinds.IsPath(action))
                return value == BrushValue.Radius && values.Defaults.Resizable;
            switch (value)
            {
                case BrushValue.Radius: return values.Defaults.Resizable;
                case BrushValue.Amount: return AmountOf(action, values) != AmountKind.None;
                case BrushValue.Hardness: return !BrushState.GridMode;
                case BrushValue.Rotation: return !BrushState.GridMode && Turnable(ShapeCycle.Effective(values));
                case BrushValue.Depth: return HasDepth(ShapeCycle.Effective(values));
                case BrushValue.Target: return TargetHeight.Used(action);
                default: return false;
            }
        }

        /// <summary>Selects the next value that applies, wrapping around.</summary>
        public static void Next(ToolAction action, BrushValues values)
        {
            int start = System.Array.IndexOf(Order, BrushState.Selected);
            for (int i = 1; i <= Order.Length; i++)
            {
                BrushValue candidate = Order[(start + i) % Order.Length];
                if (Applies(candidate, action, values))
                {
                    BrushState.Selected = candidate;
                    return;
                }
            }
            BrushState.Selected = BrushValue.Radius;
        }

        /// <summary>Moves off a value that stopped applying (another entry, shape or grid mode).</summary>
        public static void EnsureValid(ToolAction action, BrushValues values)
        {
            if (!Applies(BrushState.Selected, action, values))
                Next(action, values);
        }

        public static AmountKind AmountOf(ToolAction action, BrushValues values)
        {
            switch (action.Height)
            {
                case HeightOp.Raise:
                case HeightOp.Lower:
                case HeightOp.Offset: return AmountKind.Amount;
                case HeightOp.Level: return StyleCycle.Effective(values) == LevelStyle.Instant ? AmountKind.None : AmountKind.MaxStep;
                case HeightOp.Smooth: return AmountKind.Strength;
            }
            if (action.UsesAmount)
                return AmountKind.Amount;
            return PaintCycle.PaintsVegetation(action, values) ? AmountKind.Density : AmountKind.None;
        }

        public static bool Turnable(BrushShape shape) => shape == BrushShape.Square || shape == BrushShape.Rectangle || shape == BrushShape.Frame;

        public static bool HasDepth(BrushShape shape) => shape == BrushShape.Rectangle || shape == BrushShape.Ring || shape == BrushShape.Frame;
    }
}
