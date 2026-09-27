using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The height limits one edit meets while it is planned on one heightmap, resolved once per plan: whether the edit
    /// may ignore the limits (an admin edit the server approved) and the global limits. Per point, only when needed: the
    /// biome rules, and the dig exceptions (asked per point, as the ramp and road checks do, so a check on the sender
    /// and the write on the owner agree; the Protection module caches its answers). A limit never pushes a point back: a
    /// point already past the limit (edited before the limit was lowered) may stay where it is or move back toward the
    /// allowed range, but not further out.
    /// </summary>
    public sealed class LimitContext
    {
        private bool ignore;
        private bool exceptions;
        private bool perBiome;
        private float raise;
        private float dig;
        private float admin;
        private float absolute;

        /// <summary>
        /// An admin's edit: approved by the server (owner side) or privileged from a local admin (sender side). Its size
        /// may go past the owner's caps up to the admin ceilings (<see cref="EngineSettings.AdminMaxRadius"/>).
        /// </summary>
        public bool AdminEdit { get; private set; }

        /// <summary>Resolves the settings for the next plan.</summary>
        public void Reset(bool ignoreLimits, bool adminEdit)
        {
            ignore = ignoreLimits;
            AdminEdit = adminEdit;
            exceptions = HeightLimits.HasDigExceptions;
            perBiome = HeightLimits.HasBiomeRules;
            raise = LimitSettings.RaiseValue;
            dig = LimitSettings.DigValue;
            admin = HeightLimits.Admin;
            absolute = HeightLimits.Absolute;
        }

        /// <summary>How far a point at this world position may be raised and dug for this edit.</summary>
        public void Range(float wx, float wz, out float up, out float down)
        {
            up = raise;
            down = dig;
            if (perBiome)
                HeightLimits.At(wx, wz, out up, out down);
            if (exceptions && down < absolute && HeightLimits.DigLifted(new Vector3(wx, 0f, wz)))
                down = absolute;
            if (ignore)
            {
                up = Mathf.Max(up, admin);
                down = Mathf.Max(down, admin);
            }
        }

        public float DigAt(float wx, float wz)
        {
            Range(wx, wz, out _, out float down);
            return down;
        }

        /// <summary>The new height held within the limits around the base (widened to include where the point is now).</summary>
        public float Clamp(float wx, float wz, float baseHeight, float before, float after, out bool limited)
        {
            Range(wx, wz, out float up, out float down);
            float low = Mathf.Min(baseHeight - down, before);
            float high = Mathf.Max(baseHeight + up, before);
            float held = Mathf.Clamp(after, low, high);
            limited = Mathf.Abs(held - after) > 0.0005f;
            return held;
        }
    }
}
