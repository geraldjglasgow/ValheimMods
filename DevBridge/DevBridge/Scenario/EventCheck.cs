using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevBridge.Events;
using Newtonsoft.Json;

namespace DevBridge.Scenario
{
    /// <summary>wait_event: blocks the scenario thread until a matching game event is in the EventLog, or the time is up.</summary>
    internal static class EventCheck
    {
        private static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(500);

        /// <summary>A data value that does not serialise is left out rather than failing the step.</summary>
        private static readonly JsonSerializerSettings Safe = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) => args.ErrorContext.Handled = true,
        };

        internal static StepOutcome Wait(Step step, RunContext run)
        {
            long cursor = run.EventsFrom(step.Since);
            DateTime end = DateTime.UtcNow.AddSeconds(step.Seconds);
            while (true)
            {
                string found = Scan(ref cursor, step);
                if (found != null) return StepOutcome.Of(found, true);
                if (ScenarioRunner.Quitting) return StepOutcome.Fail("the game is shutting down");
                TimeSpan left = (end < run.Deadline ? end : run.Deadline) - DateTime.UtcNow;
                if (left <= TimeSpan.Zero) return StepOutcome.Fail(Missed(step, end > run.Deadline));
                EventLog.WaitFor(cursor, left < Slice ? left : Slice);
            }
        }

        /// <summary>The first new event that matches, as JSON; the cursor moves past every event read.</summary>
        private static string Scan(ref long cursor, Step step)
        {
            List<GameEvent> fresh = EventLog.Since(cursor, int.MaxValue, null);
            if (fresh.Count > 0) cursor = fresh[fresh.Count - 1].Seq + 1;
            foreach (GameEvent item in fresh)
            {
                if (step.Target != "*" && !string.Equals(item.Kind, step.Target, StringComparison.OrdinalIgnoreCase)) continue;
                string text = Show(item);
                if (step.Grep == null || text.IndexOf(step.Grep, StringComparison.OrdinalIgnoreCase) >= 0) return text;
            }
            return null;
        }

        /// <summary>An event as one JSON object: seq, kind, time, then its data fields (plain already: EventLog copies them);
        /// a data field named like one of the first three is kept as data_seq, data_kind or data_time.</summary>
        internal static string Show(GameEvent item)
        {
            var flat = new Dictionary<string, object>
            {
                ["seq"] = item.Seq, ["kind"] = item.Kind, ["time"] = item.Clock.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            };
            foreach (KeyValuePair<string, object> pair in item.Data)
                flat[flat.ContainsKey(pair.Key) ? "data_" + pair.Key : pair.Key] = pair.Value;
            return JsonConvert.SerializeObject(flat, Formatting.None, Safe);
        }

        private static string Missed(Step step, bool runEnded)
        {
            string what = $"no {(step.Target == "*" ? "" : step.Target + " ")}event{(step.Grep == null ? "" : $" matching '{step.Grep}'")}";
            return runEnded ? what + " before the run's time ran out" : $"{what} within {step.Seconds.ToString(CultureInfo.InvariantCulture)} s";
        }
    }
}
