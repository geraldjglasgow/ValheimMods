using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DevBridge.Scenario
{
    internal enum StepKind { Do, Eval, Wait, WaitEvent, LogClean }

    /// <summary>One step ready to run: what it does, its arguments, what it checks and where its value goes.</summary>
    internal sealed class Step
    {
        internal StepKind Kind;
        internal string Name;
        internal string Label;

        /// <summary>do: the endpoint; eval: the expression; wait_event: the event kind; log_clean: the level's name.</summary>
        internal string Target;

        /// <summary>wait: how long; wait_event: how long at most.</summary>
        internal float Seconds;

        internal readonly Dictionary<string, string> Args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal LogLevel Level;
        internal string Grep;
        internal readonly List<string> Ignore = new List<string>();

        /// <summary>wait_event and log_clean: from where to look, "run", "previous" (the step before's start) or "now".</summary>
        internal string Since;

        internal string Json;
        internal string Save;
        internal bool? ContinueOnFail;
        internal bool Optional;
        internal Expectation Expect;
    }
}
