using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>
    /// One entry's starting brush values and radius limits, resolved from the YAML entry, the YAML family, the .cfg
    /// and the entry's own action, in that order. Built when an entry is first selected (and again after the YAML
    /// file changes); the player's changes live in <see cref="BrushValues"/>.
    /// </summary>
    public sealed class EntryDefaults
    {
        public float Radius;
        public float OwnRadius;
        public float? MinRadius;
        public float? MaxRadius;
        public float Amount;
        public float MaxStep;
        public float Strength;
        public float Hardness;
        public LevelStyle Style;
        public BrushShape Shape;
        public bool Resizable;

        public static EntryDefaults Resolve(ToolAction action)
        {
            EntryRule entry = BrushRules.Entry(action.Id);
            EntryRule family = BrushRules.Family(action.Family);
            bool resizable = action.Resizable && (entry.Resizable ?? family.Resizable ?? ModdedResizable(action));
            return new EntryDefaults
            {
                OwnRadius = action.BaseRadius,
                Radius = entry.Radius ?? family.Radius ?? action.BaseRadius * (resizable ? Multiplier(action) : 1f),
                MinRadius = entry.MinRadius ?? family.MinRadius,
                MaxRadius = entry.MaxRadius ?? family.MaxRadius,
                Amount = entry.Amount ?? family.Amount ?? action.Amount,
                MaxStep = entry.MaxStep ?? family.MaxStep ?? action.MaxStep,
                Strength = entry.Strength ?? family.Strength ?? action.Strength,
                Hardness = entry.Hardness ?? family.Hardness ?? action.Hardness,
                Style = entry.Style ?? family.Style ?? DefaultStyle(action),
                Shape = entry.Shape ?? family.Shape ?? BrushShape.Circle,
                Resizable = resizable,
            };
        }

        /// <summary>The .cfg default style replaces the plain Ease of the game's and EarthWright's own level entries.</summary>
        private static LevelStyle DefaultStyle(ToolAction action)
        {
            bool plainLevel = action.Height == HeightOp.Level && action.Style == LevelStyle.Ease && action.Family != ToolFamily.Modded;
            return plainLevel ? BrushSettings.DefaultStyle.Value : action.Style;
        }

        private static bool ModdedResizable(ToolAction action)
        {
            return action.Family != ToolFamily.Modded || BrushSettings.ResizeModded.Value;
        }

        /// <summary>The global size multiplier, when its switch for this kind of entry is on.</summary>
        private static float Multiplier(ToolAction action)
        {
            if (action.IsSpecial)
                return 1f;
            bool on;
            switch (action.Height)
            {
                case HeightOp.Level: on = BrushSettings.MultiplyLevel.Value; break;
                case HeightOp.Raise:
                case HeightOp.Lower: on = BrushSettings.MultiplyRaise.Value; break;
                case HeightOp.Smooth: on = BrushSettings.MultiplySmooth.Value; break;
                case HeightOp.None: on = BrushSettings.MultiplyPaint.Value; break;
                default: on = false; break;
            }
            return on ? BrushSettings.SizeMultiplier.Value : 1f;
        }
    }
}
