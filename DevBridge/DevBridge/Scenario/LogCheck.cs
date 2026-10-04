using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using DevBridge.Logs;

namespace DevBridge.Scenario
{
    /// <summary>log_clean: fails when the BepInEx log got lines at the level or worse (warning: warnings and errors).</summary>
    internal static class LogCheck
    {
        private const int Shown = 5;

        internal static StepOutcome Run(Step step, RunContext run)
        {
            List<LineBuffer.Line> bad = LogCapture.Lines.Since(run.LogFrom(step.Since), int.MaxValue, line => Counts(line, step));
            if (bad.Count == 0) return StepOutcome.Of($"no {step.Level.ToString().ToLowerInvariant()} lines");
            string lines = string.Join("\n", bad.Take(Shown).Select(line => $"{line.Seq} {line.Text}"));
            return StepOutcome.Fail($"{bad.Count} line(s) at {step.Level.ToString().ToLowerInvariant()} or worse, first: {Fmt.Clip(bad[0].Text, 200)}", lines);
        }

        /// <summary>The same level rule as /log?level=: lines at the level or more severe, None (unknown) never.</summary>
        private static bool Counts(LineBuffer.Line line, Step step)
        {
            if (line.Level == LogLevel.None || (int)line.Level > (int)step.Level) return false;
            if (step.Grep != null && !Has(line.Text, step.Grep)) return false;
            return !step.Ignore.Any(text => Has(line.Text, text));
        }

        private static bool Has(string text, string part) => text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
