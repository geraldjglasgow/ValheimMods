using System;
using System.Collections.Generic;
using System.IO;
using DevBridge.Scenario;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/scenario: runs a scripted in-game test from a JSON file or body and replies pass or fail per step.</summary>
    internal static class ScenarioRoute
    {
        /// <summary>The HTTP thread waits timeout= plus this; the run gets timeout= so its report arrives before the wait ends.</summary>
        private static readonly TimeSpan Margin = TimeSpan.FromSeconds(5);

        private static Router routes;

        internal static void Register(Router router)
        {
            routes = router;
            Application.quitting += () => ScenarioRunner.Quitting = true;
            router.Add("/scenario",
                "/scenario?file=<scenario.json>&timeout=120   or POST the JSON itself (Content-Type: application/json)\n" +
                "                       run a scripted test: setup, steps, cleanup; each step calls an endpoint (do), evaluates (eval),\n" +
                "                       waits, waits for an event or checks the log, then checks what it got; replies pass/fail per step;\n" +
                "                       timeout= is the whole run (default 300, max 595 s); one at a time; format in DevBridge/REFERENCE.md",
                Handle);
        }

        /// <summary>Answers at once only for bad input; otherwise the scenario thread answers when the run ends.</summary>
        private static void Handle(BridgeRequest request)
        {
            string file = request.Get("file");
            string json = file != null ? Read(file) : request.Get("body")
                ?? throw new BridgeException("give file=<scenario.json>, or POST the scenario itself with Content-Type: application/json");
            ScenarioPlan plan = ScenarioParser.Parse(json, file == null ? "scenario" : Path.GetFileNameWithoutExtension(Fmt.WindowsPath(file)));
            ScenarioRunner.Start(plan, request.Patience - Margin - (DateTime.UtcNow - request.Arrived), Dispatch, request);
        }

        private static Reply Dispatch(string path, Dictionary<string, string> args) => routes.Dispatch(BridgeRequest.Make(path, args));

        private static string Read(string file)
        {
            string path = Path.GetFullPath(Fmt.WindowsPath(file));
            return File.Exists(path) ? File.ReadAllText(path) : throw new BridgeException($"no file {path}");
        }
    }
}
