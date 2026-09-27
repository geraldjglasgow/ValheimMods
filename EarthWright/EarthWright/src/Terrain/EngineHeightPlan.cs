using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The height operations of a brush stroke, one vertex at a time: from the current height and the footprint weight to
    /// the new height (local to the heightmap). Pure math over a <see cref="HeightView"/>; limits and recording are the
    /// caller's. Level styles: Ease lerps toward the target by the (eased) weight with the change capped at MaxStep;
    /// Step moves toward the target by at most MaxStep, scaled by the weight; Instant lerps by the weight (a plateau).
    /// </summary>
    public sealed class HeightPlan
    {
        public HeightView View;
        public LimitContext Limits;
        public HeightOp Op;
        public LevelStyle Style;

        /// <summary>Local target height (Level, SetMin, SetMax).</summary>
        public float Target;

        public float Amount;
        public float MaxStep;
        public float Strength;
        public float EasePower = 1f;

        public void Configure(HeightView view, LimitContext limits, StrokeParams p)
        {
            View = view;
            Limits = limits;
            Op = p.Stroke.Height;
            Style = p.Stroke.Style;
            Target = p.Stroke.Target - view.OriginY;
            Amount = p.Amount;
            MaxStep = p.MaxStep;
            Strength = p.Strength;
            EasePower = EngineSettings.EasePowerValue;
        }

        /// <summary>The new local height of vertex (x, y) with index i, or NaN to leave it.</summary>
        public float After(int x, int y, int i, float current, float weight, float wx, float wz)
        {
            switch (Op)
            {
                case HeightOp.Level: return Level(current, weight);
                case HeightOp.Raise: return current + Amount * weight;
                case HeightOp.Lower: return current - Amount * weight;
                case HeightOp.Smooth: return SmoothAt(x, y, current, weight);
                case HeightOp.Reset: return Toward(current, View.Base[i], weight);
                case HeightOp.SetMin: return current < Target ? Toward(current, Target, weight) : float.NaN;
                case HeightOp.SetMax: return current > Target ? Toward(current, Target, weight) : float.NaN;
                case HeightOp.Offset: return Toward(current, View.Base[i] + Amount, weight);
                case HeightOp.RemoveSurface: return Toward(current, View.Base[i] - Limits.DigAt(wx, wz), weight);
                default: return float.NaN;
            }
        }

        private static float Toward(float current, float target, float weight) => current + (target - current) * weight;

        private float Level(float current, float weight)
        {
            float gap = Target - current;
            switch (Style)
            {
                case LevelStyle.Instant:
                    return current + gap * weight;
                case LevelStyle.Step:
                    return current + Mathf.Clamp(gap, -MaxStep, MaxStep) * weight;
                default:
                    float eased = EasePower == 1f ? weight : Mathf.Pow(weight, EasePower);
                    return current + Mathf.Clamp(gap * eased, -MaxStep, MaxStep);
            }
        }

        /// <summary>Toward the average of the (up to 8) neighbours that are loaded; NaN when none is.</summary>
        private float SmoothAt(int x, int y, float current, float weight)
        {
            float sum = 0f;
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    float h = View.HeightAt(x + dx, y + dy);
                    if (float.IsNaN(h))
                        continue;
                    sum += h;
                    count++;
                }
            }
            return count == 0 ? float.NaN : Toward(current, sum / count, Strength * weight);
        }
    }
}
