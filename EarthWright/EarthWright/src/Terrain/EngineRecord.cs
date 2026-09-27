using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Turns a planned new height into a recorded change: held within the height limits, dropped when it is too small to
    /// matter (under a millimetre), and marked "forget" when a reset brings the point all the way back to its base, so
    /// the compiler stops carrying it. Shared by the stroke, vertex-set and gentle-slope planners.
    /// </summary>
    public static class EngineRecord
    {
        /// <summary>Height changes smaller than this (metres) are not written.</summary>
        public const float MinChange = 0.001f;

        /// <summary>Paint changes smaller than this (sum over the channels) are not written.</summary>
        public const float MinPaint = 0.002f;

        public static void Height(HeightView view, LimitContext limits, ChangeBuffer buffer, int i, float wx, float wz, float before, float after, bool reset)
        {
            float baseHeight = view.Base[i];
            bool limited = false;
            if (!reset)
                after = limits.Clamp(wx, wz, baseHeight, before, after, out limited);
            if (limited)
                buffer.HitLimit = true;
            bool forget = reset && Mathf.Abs(after - baseHeight) < MinChange;
            if (Mathf.Abs(after - before) < MinChange && !(forget && view.ModifiedAt(i)))
                return;
            buffer.AddHeight(i, before, forget ? baseHeight : after, baseHeight, limited, forget);
        }

        public static bool Differs(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) >= MinPaint;
        }
    }
}
