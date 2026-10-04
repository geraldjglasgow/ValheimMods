using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>Reads one step object of a scenario into a Step, refusing unknown kinds, keys and misspelt checks.</summary>
    internal static class StepParser
    {
        private static readonly string[] KindKeys = { "do", "eval", "wait_event", "log_clean", "wait" };
        private static readonly HashSet<string> Common = new HashSet<string>(StringComparer.Ordinal) { "name", "continue_on_fail", "optional", "json", "save" };
        private static readonly Regex SaveName = new Regex("^[A-Za-z0-9_.-]+$");
        private static readonly string[] Sinces = { "run", "previous", "now" };

        internal static Step Parse(JObject raw)
        {
            string kindKey = KindKeys.FirstOrDefault(k => raw[k] != null)
                ?? throw new BridgeException("a step needs one of do, eval, wait, wait_event or log_clean");
            var step = new Step
            {
                Kind = KindOf(kindKey), Target = JsonText.Show(raw[kindKey]), Name = Read(raw, "name"),
                Json = Read(raw, "json"), Save = Read(raw, "save"), Expect = Expectation.From(raw),
                ContinueOnFail = Bool(raw, "continue_on_fail"), Optional = Bool(raw, "optional") ?? false,
            };
            if (step.Kind == StepKind.Do || step.Kind == StepKind.Eval) ReadCall(step, raw, kindKey);
            else ReadOwn(step, raw, kindKey);
            Validate(step);
            step.Label = step.Name ?? Summary(step);
            return step;
        }

        private static StepKind KindOf(string key) =>
            key == "do" ? StepKind.Do : key == "eval" ? StepKind.Eval : key == "wait" ? StepKind.Wait
            : key == "wait_event" ? StepKind.WaitEvent : StepKind.LogClean;

        /// <summary>do and eval: every key that is not the step's own is an argument of the endpoint, and so is all of args.</summary>
        private static void ReadCall(Step step, JObject raw, string kindKey)
        {
            if (step.Kind == StepKind.Do) step.Target = Endpoint(step.Target);
            else step.Args["expr"] = step.Target;
            foreach (JProperty property in raw.Properties())
            {
                if (property.Name == kindKey || property.Name == "args" || IsStepKey(property.Name)) continue;
                if (property.Name.StartsWith("expect", StringComparison.Ordinal))
                    throw new BridgeException($"unknown check '{property.Name}'; known: {string.Join(", ", Expectation.Keys)}");
                step.Args[property.Name] = Arg(property.Value);
            }
            JToken extra = raw["args"];
            if (extra != null && !(extra is JObject)) throw new BridgeException("args must be an object of endpoint arguments");
            if (extra is JObject named) foreach (JProperty property in named.Properties()) step.Args[property.Name] = Arg(property.Value);
        }

        private static string Endpoint(string path)
        {
            path = "/" + path.Trim().Trim('/');
            if (path.Length == 1) throw new BridgeException("do needs an endpoint, like \"/console\"");
            if (string.Equals(path, "/scenario", StringComparison.OrdinalIgnoreCase)) throw new BridgeException("a scenario cannot run /scenario");
            if (string.Equals(path, "/events", StringComparison.OrdinalIgnoreCase))
                throw new BridgeException("a scenario reads events with wait_event, not do /events (it is served on the HTTP thread)");
            return path;
        }

        /// <summary>wait, wait_event and log_clean take only their own keys, so a typo is an error rather than ignored.</summary>
        private static void ReadOwn(Step step, JObject raw, string kindKey)
        {
            string[] own = step.Kind == StepKind.WaitEvent ? new[] { "grep", "timeout", "since" }
                : step.Kind == StepKind.LogClean ? new[] { "grep", "ignore", "since" } : new string[0];
            foreach (JProperty property in raw.Properties())
            {
                if (property.Name == kindKey || IsStepKey(property.Name) || own.Contains(property.Name)) continue;
                throw new BridgeException($"{kindKey} takes no '{property.Name}'" + (own.Length > 0 ? $"; it takes {string.Join(", ", own)}" : ""));
            }
            step.Grep = Read(raw, "grep");
            if (step.Kind == StepKind.Wait) step.Seconds = Seconds(raw[kindKey], kindKey);
            if (step.Kind == StepKind.WaitEvent) ReadEventWait(step, raw);
            if (step.Kind == StepKind.LogClean) ReadLogCheck(step, raw);
        }

        private static void ReadEventWait(Step step, JObject raw)
        {
            if (step.Target.Trim().Length == 0) throw new BridgeException("wait_event needs an event kind (death, hit, spawn, log...) or *");
            step.Seconds = raw["timeout"] == null ? 30f : Seconds(raw["timeout"], "timeout");
            step.Since = Since(raw, "previous");
        }

        private static void ReadLogCheck(Step step, JObject raw)
        {
            if (!Enum.TryParse(step.Target, true, out step.Level) || step.Level == LogLevel.None || step.Level == LogLevel.All)
                throw new BridgeException("log_clean takes a level: warning (warnings and errors), error or fatal");
            JToken ignore = raw["ignore"];
            if (ignore is JArray list) step.Ignore.AddRange(list.Select(JsonText.Show));
            else if (ignore != null) step.Ignore.Add(JsonText.Show(ignore));
            step.Since = Since(raw, "run");
        }

        private static bool IsStepKey(string key) => Common.Contains(key) || Expectation.Keys.Contains(key);

        private static void Validate(Step step)
        {
            if (step.Save != null && !SaveName.IsMatch(step.Save)) throw new BridgeException("save takes a name of letters, digits, _ . -");
            if (step.Json != null && !step.Json.Contains("${")) Path(step.Json);
        }

        /// <summary>Throws for a JSON path that does not parse; reading it from an empty object only parses it.</summary>
        private static void Path(string path)
        {
            try { new JObject().SelectTokens(path).ToList(); }
            catch (JsonException error) { throw new BridgeException($"json path '{path}': {error.Message}"); }
        }

        private static string Summary(Step step)
        {
            switch (step.Kind)
            {
                case StepKind.Do: return Fmt.Clip("do " + step.Target + " " + string.Join(" ", step.Args.Select(a => a.Key + "=" + a.Value)), 80);
                case StepKind.Eval: return Fmt.Clip("eval " + step.Target, 80);
                case StepKind.Wait: return $"wait {step.Seconds.ToString(CultureInfo.InvariantCulture)} s";
                case StepKind.WaitEvent: return Fmt.Clip($"wait_event {step.Target}" + (step.Grep == null ? "" : " " + step.Grep), 80);
                default: return Fmt.Clip($"log_clean {step.Target}" + (step.Grep == null ? "" : " " + step.Grep), 80);
            }
        }

        private static string Read(JObject raw, string key) => raw[key] == null || raw[key].Type == JTokenType.Null ? null : JsonText.Show(raw[key]);

        private static string Arg(JToken value) => value.Type == JTokenType.Null ? "" : JsonText.Show(value);

        private static bool? Bool(JObject raw, string key)
        {
            JToken token = raw[key];
            if (token == null) return null;
            return token.Type == JTokenType.Boolean ? (bool)token : throw new BridgeException($"{key} takes true or false");
        }

        private static float Seconds(JToken token, string key)
        {
            if (Comparison.Number(JsonText.Show(token), out double seconds) && seconds >= 0) return (float)seconds;
            throw new BridgeException($"{key} takes a number of seconds");
        }

        private static string Since(JObject raw, string fallback)
        {
            string since = Read(raw, "since") ?? fallback;
            return Sinces.Contains(since) ? since : throw new BridgeException("since takes run, previous or now");
        }
    }
}
