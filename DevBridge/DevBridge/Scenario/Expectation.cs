using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DevBridge.Server;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>The checks on a step's value; every one given must hold.</summary>
    internal sealed class Expectation
    {
        internal static readonly HashSet<string> Keys = new HashSet<string>(StringComparer.Ordinal)
            { "expect", "expect_contains", "expect_not_contains", "expect_regex", "expect_approx", "tolerance", "expect_count" };

        private const double DefaultTolerance = 0.01;

        private string expect, contains, notContains, pattern, approx, tolerance, count;

        internal static Expectation From(JObject raw)
        {
            var made = new Expectation
            {
                expect = Text(raw, "expect"), contains = Text(raw, "expect_contains"), notContains = Text(raw, "expect_not_contains"),
                pattern = Text(raw, "expect_regex"), approx = Text(raw, "expect_approx"), tolerance = Text(raw, "tolerance"),
                count = Text(raw, "expect_count"),
            };
            if (made.pattern != null && !made.pattern.Contains("${")) Compile(made.pattern);
            if (made.tolerance != null && made.approx == null) throw new BridgeException("tolerance goes with expect_approx");
            return made;
        }

        /// <summary>True when a check reads the value itself; expect_count alone needs none.</summary>
        internal bool OnValue => expect != null || contains != null || notContains != null || pattern != null || approx != null;

        /// <summary>Null when every check holds, else why not. found is how many values the step got.</summary>
        internal string Check(string got, int found)
        {
            string few = count == null ? null : Comparison.Check(count, found.ToString(CultureInfo.InvariantCulture));
            if (few != null) return "count " + few;
            if (!OnValue) return null;
            if (got == null) return "there is no value to check";
            return (expect == null ? null : Comparison.Check(expect, got)) ?? TextChecks(got) ?? Near(got);
        }

        private string TextChecks(string got)
        {
            if (contains != null && got.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0) return $"does not contain '{contains}'";
            if (notContains != null && got.IndexOf(notContains, StringComparison.OrdinalIgnoreCase) >= 0) return $"contains '{notContains}'";
            if (pattern != null && !Compile(pattern).IsMatch(got)) return $"does not match /{pattern}/";
            return null;
        }

        private string Near(string got)
        {
            if (approx == null) return null;
            if (!Comparison.Number(approx, out double want)) return $"expect_approx '{approx}' is not a number";
            double within = DefaultTolerance;
            if (tolerance != null && !Comparison.Number(tolerance, out within)) return $"tolerance '{tolerance}' is not a number";
            if (!Comparison.Number(got, out double have)) return $"'{got.Trim()}' is not a number";
            return Math.Abs(have - want) <= within ? null : $"{have.ToString(CultureInfo.InvariantCulture)} is not within {within.ToString(CultureInfo.InvariantCulture)} of {approx}";
        }

        private static Regex Compile(string text)
        {
            try { return new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2)); }
            catch (ArgumentException error) { throw new BridgeException("expect_regex: " + error.Message); }
        }

        private static string Text(JObject raw, string key) => raw[key] == null ? null : JsonText.Show(raw[key]);
    }
}
