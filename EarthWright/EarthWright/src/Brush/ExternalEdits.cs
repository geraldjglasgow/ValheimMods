using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Brush values other modules write straight into <see cref="BrushState"/>: the Preview panel's typed values,
    /// presets, choice grids and target lock, and Menu's start values of a custom entry. The brush remembers what it
    /// published last; whatever differs by the next check was written by someone else and is adopted into the entry's
    /// remembered values (clamped to the current limits) and the target state, instead of being overwritten by the next
    /// publish. Local player state only.
    /// </summary>
    public static class ExternalEdits
    {
        /// <summary>Ignores float noise (a percent field returns value * 100 / 100).</summary>
        private const float Epsilon = 0.0005f;

        private static bool recorded;
        private static float radius, radius2, rotation, hardness, amount, maxStep, strength, targetHeight;
        private static BrushShape shape;
        private static LevelStyle style;
        private static PaintOp paint;
        private static bool keep;
        private static TargetSource source;

        /// <summary>After the brush published its values.</summary>
        public static void RecordBrush()
        {
            recorded = true;
            radius = BrushState.Radius;
            radius2 = BrushState.Radius2;
            rotation = BrushState.Rotation;
            hardness = BrushState.Hardness;
            amount = BrushState.Amount;
            maxStep = BrushState.MaxStep;
            strength = BrushState.Strength;
            shape = BrushState.Shape;
            style = BrushState.Style;
            paint = BrushState.PaintOverride;
            keep = BrushState.KeepPaint;
        }

        /// <summary>After the brush published the target height.</summary>
        public static void RecordTarget()
        {
            targetHeight = BrushState.TargetHeight;
            source = BrushState.TargetSource;
        }

        /// <summary>Adopts what others wrote since the last publish. True when a brush value changed (publish again).</summary>
        public static bool Adopt(BrushValues values)
        {
            if (!recorded || values == null)
                return false;
            AdoptTarget();
            bool sizes = AdoptSizes(values);
            bool choices = AdoptChoices(values);
            return sizes || choices;
        }

        private static bool AdoptSizes(BrushValues values)
        {
            int changes = 0;
            if (Take(BrushState.Radius, radius, ref changes))
                values.Radius = BrushLimits.Radius(values, BrushState.Radius);
            if (Take(BrushState.Radius2, radius2, ref changes))
                values.Radius2 = Mathf.Max(0f, BrushState.Radius2);
            if (Take(BrushState.Hardness, hardness, ref changes))
                values.Hardness = Mathf.Clamp01(BrushState.Hardness);
            if (Take(BrushState.Amount, amount, ref changes))
                values.Amount = BrushLimits.Amount(BrushState.Amount);
            if (Take(BrushState.MaxStep, maxStep, ref changes))
                values.MaxStep = BrushLimits.MaxStep(BrushState.MaxStep);
            if (Take(BrushState.Strength, strength, ref changes))
                values.Strength = Mathf.Clamp01(BrushState.Strength);
            if (Take(BrushState.Rotation, rotation, ref changes))
                BrushPublisher.Rotation = Mathf.Repeat(BrushState.Rotation, 360f);
            return changes > 0;
        }

        private static bool Take(float now, float was, ref int changes)
        {
            if (!Differs(now, was))
                return false;
            changes++;
            return true;
        }

        private static bool AdoptChoices(BrushValues values)
        {
            bool changed = false;
            if (BrushState.Shape != shape)
            {
                values.Shape = BrushState.Shape;
                changed = true;
            }
            if (BrushState.Style != style)
            {
                values.Style = BrushState.Style;
                changed = true;
            }
            if (BrushState.PaintOverride != paint || BrushState.KeepPaint != keep)
            {
                values.Paint = PaintCycle.ChoiceOf(BrushState.PaintOverride, BrushState.KeepPaint);
                changed = true;
            }
            return changed;
        }

        /// <summary>A chosen source becomes the target mode; a typed height fixes it (Locked unless Exact or Floor).</summary>
        private static void AdoptTarget()
        {
            TargetSource now = BrushState.TargetSource;
            if (now != source)
            {
                if (TargetState.IsFixedMode(now))
                    TargetState.SetFixed(now, BrushState.TargetHeight);
                else
                    TargetState.SetLive(now);
            }
            else if (Differs(BrushState.TargetHeight, targetHeight) && TargetState.IsFixedMode(now))
            {
                TargetState.SetFixed(now, BrushState.TargetHeight);
            }
            RecordTarget();
        }

        private static bool Differs(float now, float was) => Mathf.Abs(now - was) > Epsilon;
    }
}
