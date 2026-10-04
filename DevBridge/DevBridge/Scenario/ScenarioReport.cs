using System.Collections.Generic;
using System.Linq;
using DevBridge.Events;
using DevBridge.Server;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace DevBridge.Scenario
{
    /// <summary>How one step went.</summary>
    internal sealed class StepResult
    {
        internal string Phase;
        internal int Number;
        internal string Name;
        internal bool Ok;
        internal bool Skipped;
        internal bool Optional;
        internal long Ms;
        internal string Got;
        internal string Reason;

        internal string Where => $"{Phase} {Number}";
    }

    /// <summary>The run's report as it grows: a log line per step, then the reply and a "scenario" event at the end.</summary>
    internal sealed class ScenarioReport
    {
        private const int GotLength = 200;

        private readonly string name;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<StepResult> results = new List<StepResult>();

        internal ScenarioReport(string name)
        {
            this.name = name;
        }

        internal void Started(int steps, double seconds) => Log($"started, {steps} steps, {Fmt.R((float)seconds)} s to run");

        internal void Add(StepResult result)
        {
            results.Add(result);
            if (result.Skipped) return;
            string verdict = result.Ok ? "ok" : result.Optional ? "failed (optional)" : "FAILED";
            Log($"{verdict} {result.Where} '{result.Name}' ({result.Ms} ms)" + (result.Ok ? "" : ": " + result.Reason));
        }

        internal Dictionary<string, object> Finish(string stopped, IDictionary<string, string> saved)
        {
            int ok = results.Count(r => r.Ok), skipped = results.Count(r => r.Skipped);
            int failed = results.Count(r => !r.Ok && !r.Skipped && !r.Optional);
            bool passed = failed == 0 && stopped == null;
            Log($"{(passed ? "passed" : "FAILED")}: {ok} ok, {failed} failed, {skipped} skipped in {clock.ElapsedMilliseconds} ms"
                + (stopped == null ? "" : ", stopped: " + stopped));
            EventLog.Add("scenario", new Dictionary<string, object>
            {
                ["name"] = name, ["passed"] = passed, ["ok"] = ok, ["failed"] = failed, ["skipped"] = skipped, ["ms"] = clock.ElapsedMilliseconds,
            });
            return new Dictionary<string, object>
            {
                ["name"] = name, ["passed"] = passed, ["ms"] = clock.ElapsedMilliseconds,
                ["ok"] = ok, ["failed"] = failed, ["skipped"] = skipped, ["stopped"] = stopped,
                ["steps"] = results.Select(Show).ToList(),
                ["saved"] = saved.ToDictionary(pair => pair.Key, pair => Fmt.Clip(pair.Value, GotLength)),
            };
        }

        private static Dictionary<string, object> Show(StepResult result)
        {
            var shown = new Dictionary<string, object> { ["phase"] = result.Phase, ["step"] = result.Number, ["name"] = result.Name };
            if (result.Skipped) shown["skipped"] = true;
            else
            {
                shown["ok"] = result.Ok;
                shown["ms"] = result.Ms;
                shown["got"] = Fmt.Clip(result.Got, GotLength);
                if (!result.Ok) shown["reason"] = result.Reason;
                if (!result.Ok && result.Optional) shown["optional"] = true;
            }
            return shown;
        }

        /// <summary>Unity hands only main-thread log calls to BepInEx, so the line is written from there.</summary>
        private void Log(string text)
        {
            string line = $"[DevBridge] scenario {name}: {text}";
            MainThread.Post(() => UnityEngine.Debug.Log(line));
        }
    }
}
