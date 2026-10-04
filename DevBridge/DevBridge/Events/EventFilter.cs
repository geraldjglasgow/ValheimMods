using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Events
{
    /// <summary>Which events a caller asked for: kinds= (a comma list; -kind leaves one out) and grep= (text anywhere in
    /// the event's JSON line, any case). Runs on HTTP threads, on plain data only.</summary>
    internal sealed class EventFilter
    {
        private readonly HashSet<string> only = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> without = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string grep;

        internal EventFilter(string kinds, string grep)
        {
            this.grep = grep;
            foreach (string kind in (kinds ?? "").Split(',').Select(k => k.Trim()).Where(k => k.Length > 0))
            {
                if (kind.StartsWith("-")) without.Add(kind.Substring(1));
                else only.Add(kind);
            }
        }

        internal bool Keep(GameEvent e)
        {
            if (only.Count > 0 && !only.Contains(e.Kind)) return false;
            if (without.Contains(e.Kind)) return false;
            return grep == null || EventJson.Line(e).IndexOf(grep, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>The filter as the caller gave it, for the stream's opening line.</summary>
        internal Dictionary<string, object> Describe() => new Dictionary<string, object>
        {
            ["kinds"] = only.Count == 0 ? "all" : string.Join(",", only),
            ["without"] = string.Join(",", without),
            ["grep"] = grep,
        };
    }
}
