using System;
using System.Collections.Generic;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// The numeric core of every roll (rarity.md section 4, affixes.md "Value types"), free of game types so a scratch
    /// harness can compile it on its own and check the distributions. Pure: every random number comes from the
    /// <see cref="Random"/> passed in, so a seeded source reproduces a roll exactly.
    /// </summary>
    internal static class RollMath
    {
        /// <summary>The highest affix tier shipped (7 = ashlands; 8 deep_north is reserved).</summary>
        public const int MaxTier = 7;

        public static int ClampCeiling(int ceiling) => Math.Min(Math.Max(ceiling, 1), MaxTier);

        /// <summary>A stone's floor, clamped down to the ceiling (item-tier.md section 6); 0 = none.</summary>
        public static int EffectiveFloor(int ceiling, int floor) => floor <= 0 ? 0 : Math.Min(floor, ceiling);

        /// <summary><c>low = max(ceiling - window + 1, floor, 1)</c>; the window's top is the ceiling.</summary>
        public static int WindowLow(int ceiling, int window, int floor) =>
            Math.Max(Math.Max(ceiling - Math.Max(window, 1) + 1, floor), 1);

        /// <summary>
        /// A value uniform in [min, max] at the tier's decimals: every representable step (1, 0.1 or 0.01 apart)
        /// between the bounds is equally likely, both bounds included, and the result never leaves the bounds.
        /// Judgement call: drawing the steps directly rather than rounding a continuous draw, which would give each
        /// bound half the chance of an inner value.
        /// </summary>
        public static float RollValue(float min, float max, int decimals, Random random)
        {
            if (max <= min)
            {
                return min;
            }
            decimals = Math.Min(Math.Max(decimals, 0), 4);
            double scale = Math.Pow(10, decimals);
            long low = (long)Math.Round(min * scale, MidpointRounding.AwayFromZero);
            long high = (long)Math.Round(max * scale, MidpointRounding.AwayFromZero);
            long step = low + (long)Math.Floor(random.NextDouble() * (high - low + 1));
            step = Math.Min(Math.Max(step, low), high);
            float value = (float)Math.Round(step / scale, decimals, MidpointRounding.AwayFromZero);
            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>Index of a weighted pick; weights at or below 0 never win. -1 when nothing can win.</summary>
        public static int PickWeighted(IReadOnlyList<float> weights, Random random)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                total += Math.Max(weights[i], 0f);
            }
            if (total <= 0)
            {
                return -1;
            }
            double target = random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                float weight = Math.Max(weights[i], 0f);
                if (weight > 0f && target < weight)
                {
                    return i;
                }
                target -= weight;
            }
            return LastPositive(weights);
        }

        /// <summary>
        /// The affix count of a fresh roll: uniform in [min, max], or by <c>rolling.count_weights</c> when the rarity
        /// has a row (counts outside [min, max] are ignored; a row with no usable weight falls back to uniform).
        /// </summary>
        public static int PickCount(int min, int max, IReadOnlyDictionary<int, float>? weights, Random random)
        {
            if (max <= min)
            {
                return Math.Max(min, 0);
            }
            if (weights != null)
            {
                List<float> row = new List<float>(max - min + 1);
                for (int count = min; count <= max; count++)
                {
                    row.Add(weights.TryGetValue(count, out float w) ? w : 0f);
                }
                int index = PickWeighted(row, random);
                if (index >= 0)
                {
                    return min + index;
                }
            }
            return random.Next(min, max + 1);
        }

        /// <summary>Where a value sits in its tier, 0 (worst) to 1 (best); a single-value tier or a flag is 1.</summary>
        public static float Position(float value, float min, float max) =>
            max <= min ? 1f : Math.Min(Math.Max((value - min) / (max - min), 0f), 1f);

        private static int LastPositive(IReadOnlyList<float> weights)
        {
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                if (weights[i] > 0f)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
