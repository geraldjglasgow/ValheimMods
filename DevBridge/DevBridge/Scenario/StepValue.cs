using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>What a step's action produced: its text (JSON or not), or why the action itself failed.</summary>
    internal sealed class StepOutcome
    {
        internal string Text;
        internal bool Json;
        internal string Failure;

        internal static StepOutcome Of(string text, bool json = false) => new StepOutcome { Text = text, Json = json };

        internal static StepOutcome Fail(string why, string text = null) => new StepOutcome { Failure = why, Text = text };
    }

    /// <summary>
    /// The value a step's checks read and its save keeps: the reply (JSON made compact), or what its json path picks from it;
    /// and how many values there are, for expect_count.
    /// </summary>
    internal sealed class StepValue
    {
        internal string Text;
        internal int Count;
        internal string Problem;

        internal static StepValue Of(StepOutcome outcome, string path)
        {
            JToken root = outcome.Json ? JsonText.TryParse(outcome.Text) : null;
            if (path == null) return root == null ? Plain(outcome.Text) : Picked(new List<JToken> { root });
            if (root == null) return new StepValue { Problem = $"json path {path} needs a JSON reply; this one is text" };
            return Picked(root.SelectTokens(path).ToList());
        }

        /// <summary>One value as itself (a list counts its items), several as a JSON list, none as no value at all.</summary>
        private static StepValue Picked(List<JToken> picked)
        {
            if (picked.Count == 0) return new StepValue();
            if (picked.Count > 1) return new StepValue { Text = new JArray(picked).ToString(Formatting.None), Count = picked.Count };
            return new StepValue { Text = JsonText.Show(picked[0]), Count = picked[0] is JArray list ? list.Count : 1 };
        }

        private static StepValue Plain(string text) => new StepValue
        {
            Text = text,
            Count = (text ?? "").Split('\n').Count(line => line.Trim().Length > 0),
        };

        /// <summary>The value part of an /eval reply: "Single = 25" gives 25, a string loses its quotes, a bool is true or false.</summary>
        internal static string EvalValue(string reply)
        {
            string first = (reply ?? "").Split('\n')[0].TrimEnd('\r');
            int equals = first.IndexOf(" = ", StringComparison.Ordinal);
            if (equals < 0) return first.Trim();
            string type = first.Substring(0, equals), value = first.Substring(equals + 3);
            if (type == "Boolean") return value.ToLowerInvariant();
            bool quoted = type == "String" && value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
            return quoted ? value.Substring(1, value.Length - 2) : value;
        }
    }
}
