using System;
using System.Globalization;
using System.Linq;

namespace DevBridge.Scenario
{
    /// <summary>
    /// An expect text: &gt;N, &gt;=N, &lt;N, &lt;=N, ==V, !=V, or a plain V meaning ==V. Numbers compare as numbers, anything
    /// else as text ignoring case and the spaces around it.
    /// </summary>
    internal static class Comparison
    {
        private static readonly string[] Operators = { ">=", "<=", "==", "!=", ">", "<" };

        /// <summary>Null when got satisfies the spec, else why not.</summary>
        internal static string Check(string spec, string got)
        {
            string op = Operators.FirstOrDefault(o => spec.StartsWith(o, StringComparison.Ordinal));
            string wanted = (op == null ? spec : spec.Substring(op.Length)).Trim();
            op = op ?? "==";
            got = (got ?? "").Trim();
            bool numbers = Number(got, out double have) & Number(wanted, out double want);
            if (op == "==" || op == "!=")
            {
                bool same = numbers ? have == want : string.Equals(got, wanted, StringComparison.OrdinalIgnoreCase);
                if (same == (op == "==")) return null;
                return op == "==" ? $"'{got}' is not '{wanted}'" : $"'{got}' should not be '{wanted}'";
            }
            if (!numbers) return $"{op} {wanted} needs numbers, got '{got}'";
            return Holds(op, have, want) ? null : $"{got} is not {op} {wanted}";
        }

        internal static bool Number(string text, out double value) =>
            double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool Holds(string op, double have, double want)
        {
            switch (op)
            {
                case ">": return have > want;
                case ">=": return have >= want;
                case "<": return have < want;
                default: return have <= want;
            }
        }
    }
}
