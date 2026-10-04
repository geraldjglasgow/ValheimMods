using System;
using System.Collections.Generic;
using DevBridge.Events;
using DevBridge.Logs;
using DevBridge.Server;

namespace DevBridge.Scenario
{
    /// <summary>Calls an endpoint the way an HTTP request would, waiting for its reply.</summary>
    internal delegate Reply Dispatcher(string path, Dictionary<string, string> args);

    /// <summary>What the steps of one run share: the dispatcher, saved values, the current deadline, and event and log marks.</summary>
    internal sealed class RunContext
    {
        internal readonly Dispatcher Dispatch;
        internal readonly Dictionary<string, string> Saved = new Dictionary<string, string>(StringComparer.Ordinal);
        internal readonly long RunEvents = EventLog.Next, RunLog = LogCapture.Lines.Next;

        /// <summary>Where the current phase must stop (UTC).</summary>
        internal DateTime Deadline;

        private long previousEvents, previousLog, stepEvents, stepLog;

        internal RunContext(Dispatcher dispatch)
        {
            Dispatch = dispatch;
            previousEvents = stepEvents = RunEvents;
            previousLog = stepLog = RunLog;
        }

        internal TimeSpan Left => Deadline - DateTime.UtcNow;

        /// <summary>Why no more can be done now, or null.</summary>
        internal string Halt => ScenarioRunner.Quitting ? "the game is shutting down" : Left <= TimeSpan.Zero ? "out of time" : null;

        /// <summary>Called as each step starts, so "previous" means the start of the step before.</summary>
        internal void StepStarts()
        {
            previousEvents = stepEvents;
            previousLog = stepLog;
            stepEvents = EventLog.Next;
            stepLog = LogCapture.Lines.Next;
        }

        internal long EventsFrom(string since) => since == "run" ? RunEvents : since == "now" ? stepEvents : previousEvents;

        internal long LogFrom(string since) => since == "run" ? RunLog : since == "now" ? stepLog : previousLog;
    }
}
