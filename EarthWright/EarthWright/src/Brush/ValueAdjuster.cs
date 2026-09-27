using BepInEx.Configuration;
using EarthWright.Actions;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Changes the selected brush value by one step up or down. Each change starts from the published (clamped) value,
    /// so a value never has to be wound back from beyond a limit. The wheel honours negative step settings (reversed
    /// direction); the keys always step the way they say. Local player state only.
    /// </summary>
    public static class ValueAdjuster
    {
        /// <param name="direction">+1 or -1 (several notches in one frame add up).</param>
        /// <param name="fast">The fast modifier is held.</param>
        /// <param name="wheel">The change comes from the mouse wheel.</param>
        public static void Step(ToolAction action, BrushValues values, int direction, bool fast, bool wheel)
        {
            if (direction == 0)
                return;
            float mult = fast ? BrushSettings.FastMultiplier.Value : 1f;
            switch (BrushState.Selected)
            {
                case BrushValue.Radius: StepRadius(values, direction, mult, wheel); break;
                case BrushValue.Amount: StepAmount(action, values, direction, mult, wheel); break;
                case BrushValue.Hardness: StepHardness(values, direction, mult, wheel); break;
                case BrushValue.Rotation: StepRotation(direction, mult, wheel); break;
                case BrushValue.Depth: StepDepth(values, direction, mult, wheel); break;
                case BrushValue.Target: TargetState.AdjustExact(direction * (fast ? TargetSettings.ExactFastStep.Value : TargetSettings.ExactStep.Value)); break;
            }
            BrushPublisher.Publish(action, values);
            BrushAnnounce.Value(BrushHudText.SelectedValue(action, values));
        }

        public static float StepOf(ConfigEntry<float> setting, bool wheel) => wheel ? setting.Value : Mathf.Abs(setting.Value);

        private static void StepRadius(BrushValues values, int direction, float mult, bool wheel)
        {
            float step = StepOf(BrushSettings.RadiusStep, wheel) * mult;
            if (BrushState.GridMode)
                step = Mathf.Sign(step) * Mathf.Max(1f, Mathf.Abs(step));
            values.Radius = BrushLimits.Radius(values, BrushState.Radius + direction * step);
        }

        private static void StepAmount(ToolAction action, BrushValues values, int direction, float mult, bool wheel)
        {
            float step = StepOf(BrushSettings.AmountStep, wheel) * mult * direction;
            switch (ValueSelector.AmountOf(action, values))
            {
                case AmountKind.Amount: values.Amount = BrushLimits.Amount(BrushState.Amount + step); break;
                case AmountKind.MaxStep: values.MaxStep = BrushLimits.MaxStep(BrushState.MaxStep + BrushLimits.MaxStepIncrement(BrushState.MaxStep, step)); break;
                case AmountKind.Strength: values.Strength = Mathf.Clamp01(BrushState.Strength + step); break;
                case AmountKind.Density: BrushState.Density = Mathf.Clamp01(BrushState.Density + step); break;
            }
        }

        private static void StepHardness(BrushValues values, int direction, float mult, bool wheel)
        {
            float step = StepOf(BrushSettings.HardnessStep, wheel) * mult;
            values.Hardness = Mathf.Clamp01(Mathf.Round((BrushState.Hardness + direction * step) * 1000f) / 1000f);
        }

        private static void StepRotation(int direction, float mult, bool wheel)
        {
            Turn(direction * StepOf(BrushSettings.RotationStep, wheel) * mult);
        }

        /// <summary>Turns the footprint by degrees, kept within 0..360.</summary>
        public static void Turn(float degrees)
        {
            BrushPublisher.Rotation = Mathf.Repeat(BrushPublisher.Rotation + degrees, 360f);
        }

        private static void StepDepth(BrushValues values, int direction, float mult, bool wheel)
        {
            float step = StepOf(BrushSettings.DepthStep, wheel) * mult;
            if (BrushState.GridMode)
                step = Mathf.Sign(step) * Mathf.Max(1f, Mathf.Abs(step));
            BrushLimits.RadiusRange(values.Defaults, out _, out float maxRadius);
            values.Radius2 = BrushLimits.Radius2(BrushState.Shape, BrushState.Radius, BrushState.Radius2 + direction * step, maxRadius);
        }
    }
}
