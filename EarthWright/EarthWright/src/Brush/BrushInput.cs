using EarthWright.Actions;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The brush keys, read once a frame while a terrain entry is selected and the input is free: the value selector,
    /// the wheel and the increase/decrease keys, rotation, and the toggles (shape, style, paint, grid, aim at edge).
    /// Ramps and roads keep the selector, size, paint and target keys; the shape, style, grid and edge keys are left to
    /// them (the ramp tool uses the shape key for its profiles). Local player state only.
    /// </summary>
    public static class BrushInput
    {
        public static void Update(Player player, ToolAction action, BrushValues values)
        {
            if (Keys.Pressed(ControlSettings.NextValueKey))
                ValueSelector.Next(action, values);
            ValueSelector.EnsureValid(action, values);
            Values(action, values);
            Toggles(action, values);
            if (!EntryKinds.IsPath(action))
                Rotation();
            TargetInput.Update();
            HardLevel.Update(player, action);
            GamepadInput.Update(action, values);
        }

        /// <summary>The wheel (with the modifier) and the increase/decrease keys step the selected value.</summary>
        private static void Values(ToolAction action, BrushValues values)
        {
            int wheel = ScrollInput.Notches();
            bool fast = Keys.Held(ControlSettings.FastModifier) && ControlSettings.FastModifier.Value.MainKey != ControlSettings.AdjustModifier.Value.MainKey;
            if (wheel != 0)
                ValueAdjuster.Step(action, values, wheel, fast, true);
            int keys = (RepeatKey.Fire(ControlSettings.IncreaseKey) ? 1 : 0) - (RepeatKey.Fire(ControlSettings.DecreaseKey) ? 1 : 0);
            if (keys != 0)
                ValueAdjuster.Step(action, values, keys, Keys.Held(ControlSettings.FastModifier), false);
        }

        private static void Toggles(ToolAction action, BrushValues values)
        {
            if (Keys.Pressed(ControlSettings.PaintKey))
                PaintCycle.Next(action, values);
            if (EntryKinds.IsPath(action))
                return;
            if (Keys.Pressed(ControlSettings.ShapeKey))
                ShapeCycle.Next(values);
            if (Keys.Pressed(ControlSettings.StyleKey))
                StyleCycle.Next(action, values);
            if (Keys.Pressed(ControlSettings.GridKey))
            {
                BrushState.GridMode = !BrushState.GridMode;
                BrushAnnounce.Toggle(BrushWords.Grid + " " + BrushWords.OnOff(BrushState.GridMode));
            }
            if (Keys.Pressed(ControlSettings.EdgeKey))
            {
                BrushState.AimAtEdge = !BrushState.AimAtEdge;
                BrushAnnounce.Toggle(BrushWords.AimEdge + " " + BrushWords.OnOff(BrushState.AimAtEdge));
            }
        }

        private static void Rotation()
        {
            if (BrushState.GridMode || !ValueSelector.Turnable(BrushState.Shape))
                return;
            float step = Mathf.Abs(BrushSettings.RotationStep.Value);
            if (RepeatKey.Fire(ControlSettings.RotateRightKey))
                ValueAdjuster.Turn(step);
            if (RepeatKey.Fire(ControlSettings.RotateLeftKey))
                ValueAdjuster.Turn(-step);
            if (Keys.Pressed(ControlSettings.ResetRotationKey))
                BrushPublisher.Rotation = 0f;
        }
    }
}
