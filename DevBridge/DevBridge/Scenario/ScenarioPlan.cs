using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>A step as written in the file; it is read again, with saved values substituted, when its turn comes.</summary>
    internal sealed class PlanStep
    {
        internal string Phase;
        internal int Number;
        internal JObject Raw;
        internal string Label;
        internal bool? ContinueOnFail;
        internal bool Optional;
    }

    /// <summary>A loaded scenario: its name, its three phases, and whether a failed step stops the run.</summary>
    internal sealed class ScenarioPlan
    {
        internal string Name;
        internal bool ContinueOnFail;
        internal List<PlanStep> Setup, Steps, Cleanup;

        internal int Count => Setup.Count + Steps.Count + Cleanup.Count;
    }

    /// <summary>Reads a scenario file, checking every step before anything runs so a typo fails at once, not half-way.</summary>
    internal static class ScenarioParser
    {
        private static readonly HashSet<string> Keys = new HashSet<string>(StringComparer.Ordinal)
            { "name", "description", "setup", "steps", "cleanup", "continue_on_fail" };

        /// <summary>An object with setup, steps and cleanup, or just a list of steps.</summary>
        internal static ScenarioPlan Parse(string json, string fallbackName)
        {
            JToken root = Read(json);
            JObject scenario = root as JObject ?? (root is JArray list ? new JObject { ["steps"] = list } : null)
                ?? throw new BridgeException("a scenario is a JSON object with steps (or a list of steps)");
            string unknown = scenario.Properties().Select(p => p.Name).FirstOrDefault(name => !Keys.Contains(name));
            if (unknown != null) throw new BridgeException($"unknown scenario key '{unknown}'; known: {string.Join(", ", Keys)}");
            JToken stops = scenario["continue_on_fail"];
            if (stops != null && stops.Type != JTokenType.Boolean) throw new BridgeException("continue_on_fail takes true or false");
            var plan = new ScenarioPlan
            {
                Name = scenario["name"] == null ? fallbackName : JsonText.Show(scenario["name"]), ContinueOnFail = stops != null && (bool)stops,
                Setup = Phase(scenario, "setup"), Steps = Phase(scenario, "steps"), Cleanup = Phase(scenario, "cleanup"),
            };
            if (plan.Setup.Count + plan.Steps.Count == 0) throw new BridgeException("the scenario has no setup or steps");
            return plan;
        }

        private static JToken Read(string json)
        {
            try { return JsonText.Parse(json); }
            catch (JsonException error) { throw new BridgeException("the scenario is not valid JSON: " + error.Message); }
        }

        private static List<PlanStep> Phase(JObject scenario, string phase)
        {
            JToken steps = scenario[phase];
            if (steps == null) return new List<PlanStep>();
            if (!(steps is JArray list)) throw new BridgeException($"{phase} must be a list of steps");
            return list.Select((token, index) => Planned(token, phase, index + 1)).ToList();
        }

        private static PlanStep Planned(JToken token, string phase, int number)
        {
            if (!(token is JObject raw)) throw new BridgeException($"{phase} {number}: a step is a JSON object");
            try
            {
                Step step = StepParser.Parse(raw);
                return new PlanStep { Phase = phase, Number = number, Raw = raw, Label = step.Label, ContinueOnFail = step.ContinueOnFail, Optional = step.Optional };
            }
            catch (BridgeException error)
            {
                throw new BridgeException($"{phase} {number}: {error.Message}");
            }
        }
    }
}
