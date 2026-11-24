using System;
using System.Collections.Generic;

namespace DevBridge.Load
{
    /// <summary>The smallest, mean and largest of some numbers, for the /load replies; empty reads as null.</summary>
    internal sealed class Stat
    {
        private double min = double.MaxValue;
        private double max = double.MinValue;
        private double sum;

        internal int Count { get; private set; }

        internal double Sum => sum;

        internal void Add(double value)
        {
            if (value < min) min = value;
            if (value > max) max = value;
            sum += value;
            Count++;
        }

        internal static Stat Of(List<double> values)
        {
            var stat = new Stat();
            foreach (double value in values) stat.Add(value);
            return stat;
        }

        internal Dictionary<string, object> Json() => Count == 0 ? null : new Dictionary<string, object>
        {
            ["min"] = R(min),
            ["avg"] = R(sum / Count),
            ["max"] = R(max),
        };

        /// <summary>The same with the last value of a series.</summary>
        internal Dictionary<string, object> WithLast(List<double> values)
        {
            Dictionary<string, object> json = Json();
            if (json != null) json["last"] = R(values[values.Count - 1]);
            return json;
        }

        /// <summary>Nearest-rank percentile of a list (sorted on a copy).</summary>
        internal static double Percentile(List<float> values, double q)
        {
            if (values.Count == 0) return 0;
            var sorted = new List<float>(values);
            sorted.Sort();
            int rank = (int)Math.Ceiling(q * sorted.Count) - 1;
            return R(sorted[Math.Max(0, Math.Min(sorted.Count - 1, rank))]);
        }

        internal static double R(double value) => Math.Round(value, 2);
    }
}
