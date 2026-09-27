using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The ranges brush values are clamped to. The radius takes the smallest of the .cfg / YAML maximum, the tool level
    /// cap (<see cref="BrushCaps.LevelMaxRadius"/>, set by Gear) and the skill cap; the other values have fixed or
    /// synced ranges. Grid mode rounds sizes to whole metres. Runs on the player's machine every frame.
    /// </summary>
    public static class BrushLimits
    {
        public const float MinMaxStep = 0.1f;
        public const float MaxMaxStep = 1000f;

        /// <summary>The radius range of an entry now (the minimum never exceeds the maximum).</summary>
        public static void RadiusRange(EntryDefaults d, out float min, out float max)
        {
            max = d.MaxRadius ?? BrushSettings.MaxRadius.Value;
            max = Mathf.Min(max, Safe.Call("BrushCaps.LevelMaxRadius", LevelCap, float.MaxValue));
            max = Mathf.Min(max, SkillCap.Cap(d.OwnRadius));
            max = Mathf.Max(max, 0.25f);
            min = Mathf.Min(d.MinRadius ?? BrushSettings.MinRadius.Value, max);
        }

        private static float LevelCap() => BrushCaps.LevelMaxRadius();

        /// <summary>The radius the brush uses for these values; an entry that is not resizable keeps its own size.</summary>
        public static float Radius(BrushValues values, float wanted)
        {
            EntryDefaults d = values.Defaults;
            if (!d.Resizable)
                return d.Radius;
            RadiusRange(d, out float min, out float max);
            float radius = Mathf.Clamp(wanted, min, max);
            if (!BrushState.GridMode)
                return radius;
            float upper = max < 1f ? max : Mathf.Floor(max);
            return Mathf.Clamp(Mathf.Round(radius), Mathf.Min(1f, upper), upper);
        }

        /// <summary>Rectangle half depth, ring inner radius or frame band width for the shape and outer radius.</summary>
        public static float Radius2(BrushShape shape, float radius, float wanted, float maxRadius)
        {
            bool grid = BrushState.GridMode;
            switch (shape)
            {
                case BrushShape.Rectangle:
                    return Mathf.Clamp(grid ? Mathf.Round(wanted) : wanted, grid ? 1f : 0.5f, Mathf.Max(1f, maxRadius));
                case BrushShape.Ring:
                    float inner = Mathf.Clamp(wanted, 0f, Mathf.Max(0f, radius - 0.5f));
                    return grid ? Mathf.Floor(inner) : inner;
                case BrushShape.Frame:
                    return Mathf.Clamp(grid ? Mathf.Round(wanted) : wanted, grid ? 1f : 0.5f, Mathf.Max(0.5f, radius));
                default:
                    return wanted;
            }
        }

        public static float Amount(float wanted)
        {
            float min = BrushSettings.MinAmount.Value;
            return Mathf.Clamp(wanted, min, Mathf.Max(min, BrushSettings.MaxAmount.Value));
        }

        public static float MaxStep(float wanted) => Mathf.Clamp(wanted, MinMaxStep, MaxMaxStep);

        /// <summary>The level max step moves in finer steps near 0 and coarser ones above 2 m and 20 m.</summary>
        public static float MaxStepIncrement(float current, float step)
        {
            if (current >= 20f)
                return Mathf.Sign(step) * 10f;
            if (current >= 2f)
                return Mathf.Sign(step) * 1f;
            return step;
        }
    }
}
