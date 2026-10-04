using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DevBridge.Server;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>${name} in a step's text replaced by the value an earlier step saved under that name.</summary>
    internal static class Substitution
    {
        private static readonly Regex Reference = new Regex(@"\$\{([A-Za-z0-9_.-]+)\}");

        /// <summary>A copy of the step with every string value substituted; the step as written stays untouched.</summary>
        internal static JObject Apply(JObject raw, IDictionary<string, string> saved)
        {
            var copy = (JObject)raw.DeepClone();
            foreach (JValue value in copy.Descendants().OfType<JValue>().Where(v => v.Type == JTokenType.String).ToList())
                value.Value = Text((string)value.Value, saved);
            return copy;
        }

        internal static string Text(string text, IDictionary<string, string> saved) => Reference.Replace(text, match =>
            saved.TryGetValue(match.Groups[1].Value, out string value)
                ? value
                : throw new BridgeException($"nothing was saved as {match.Groups[1].Value} (no earlier step with \"save\": \"{match.Groups[1].Value}\" got a value)"));
    }
}
