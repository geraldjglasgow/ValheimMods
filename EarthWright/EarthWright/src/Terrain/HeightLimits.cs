using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// How far the ground may be raised above or dug below the world's generated height at a position: the global
    /// settings of section 6, overridden per biome by EarthWright.Limits*.yml, the dig limit lifted by exceptions the
    /// Protection module registers. Read by the engine (every write, on the owner), the patched game clamps, the ramp and
    /// road checks and the preview (on the sender). All inputs are synced, so every machine gets the same answer.
    /// </summary>
    public static class HeightLimits
    {
        private static readonly List<Func<Vector3, bool>> digExceptions = new List<Func<Vector3, bool>>();
        private static readonly float[] biomeRaise = NaNs();
        private static readonly float[] biomeDig = NaNs();
        private static bool hasBiomeRules;
        private static float biomeMax;

        /// <summary>Metres the ground may rise above the generated height here.</summary>
        public static float Raise(Vector3 world)
        {
            At(world.x, world.z, out float raise, out _);
            return raise;
        }

        /// <summary>Metres the ground may sink below the generated height here (positive number).</summary>
        public static float Dig(Vector3 world)
        {
            if (DigLifted(world))
                return Absolute;
            At(world.x, world.z, out _, out float dig);
            return dig;
        }

        /// <summary>The largest limit any edit may reach (privileged edits, lifted dig limits); also the display clamp.</summary>
        public static float Absolute
        {
            get
            {
                float settings = Mathf.Max(Mathf.Max(LimitSettings.RaiseValue, LimitSettings.DigValue), LimitSettings.AdminValue);
                return Mathf.Max(settings, biomeMax);
            }
        }

        /// <summary>The most an approved privileged edit that ignores the limits may raise or dig.</summary>
        public static float Admin => LimitSettings.AdminValue;

        /// <summary>Adds a rule that lifts the dig limit at a position (Protection: near ore, buried treasure, in tar).</summary>
        public static void AddDigException(Func<Vector3, bool> lifts) => digExceptions.Add(lifts);

        public static bool DigLifted(Vector3 world)
        {
            foreach (Func<Vector3, bool> rule in digExceptions)
            {
                try
                {
                    if (rule(world))
                        return true;
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogError("EarthWright dig exception rule: " + e);
                }
            }
            return false;
        }

        /// <summary>A module registered a dig exception (per-point exception checks are needed).</summary>
        public static bool HasDigExceptions => digExceptions.Count > 0;

        /// <summary>Some biome overrides the global limits (per-point biome lookups are needed).</summary>
        public static bool HasBiomeRules => hasBiomeRules;

        /// <summary>The raise and dig limits at a world position from the settings and the biome rules, without dig exceptions.</summary>
        public static void At(float x, float z, out float raise, out float dig)
        {
            raise = LimitSettings.RaiseValue;
            dig = LimitSettings.DigValue;
            if (!hasBiomeRules)
                return;
            int slot = LimitBiomes.Slot(LimitBiomes.At(x, z));
            if (slot < 0)
                return;
            if (!float.IsNaN(biomeRaise[slot]))
                raise = biomeRaise[slot];
            if (!float.IsNaN(biomeDig[slot]))
                dig = biomeDig[slot];
        }

        /// <summary>Takes the per-biome rules (by slot) of a freshly parsed EarthWright.Limits*.yml.</summary>
        internal static void SetBiomeRules(BiomeLimit[] rules)
        {
            biomeMax = 0f;
            hasBiomeRules = false;
            for (int slot = 0; slot < LimitBiomes.Slots; slot++)
            {
                BiomeLimit rule = slot < rules.Length ? rules[slot] : BiomeLimit.None;
                biomeRaise[slot] = rule.Raise;
                biomeDig[slot] = rule.Dig;
                hasBiomeRules |= !float.IsNaN(rule.Raise) || !float.IsNaN(rule.Dig);
                biomeMax = Mathf.Max(biomeMax, Mathf.Max(NaNToZero(rule.Raise), NaNToZero(rule.Dig)));
            }
        }

        private static float NaNToZero(float value) => float.IsNaN(value) ? 0f : value;

        private static float[] NaNs()
        {
            float[] values = new float[LimitBiomes.Slots];
            for (int i = 0; i < values.Length; i++)
                values[i] = float.NaN;
            return values;
        }
    }
}
