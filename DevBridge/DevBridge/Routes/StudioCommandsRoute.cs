using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Studio;

namespace DevBridge.Routes
{
    /// <summary>
    /// The studio's Commands tab (also usable from curl): run console commands, list the console's commands, and the
    /// favourites and groups both the Items and Commands tabs keep (<see cref="StudioSaved"/>).
    /// </summary>
    internal static class StudioCommandsRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/console", "/studio/console?cmd=<commands, one per line, or POST them>&wait=0.3" + Pad +
                "run console commands as if typed, in order a frame apart: each with what the console printed after it", Run);
            router.Add("/studio/commands", "/studio/commands       the console's commands, the game's and mods', with their descriptions", Commands);
            router.Add("/studio/saved", "/studio/saved          the Items and Commands tabs' favourites and groups (DevBridge.studio.json beside the .cfg)",
                request => request.Json(StudioSaved.Read()));
            router.Add("/studio/saved/save", "/studio/saved/save     (POST the JSON object) replace the favourites and groups",
                request => request.Json(StudioSaved.Write(request.Get("body"))));
        }

        private static void Run(BridgeRequest request)
        {
            string commands = request.Get("cmd") ?? request.Get("body") ?? throw new BridgeException("give cmd=<console commands>");
            ConsoleRuns.Start(request, commands, ran => request.Json(new Dictionary<string, object>
            {
                ["ran"] = ran.Select(one => new Dictionary<string, object> { ["command"] = one.Command, ["printed"] = one.Printed }).ToList(),
            }));
        }

        private static void Commands(BridgeRequest request) => request.Json(Terminal.commands.Values
            .Where(command => !command.IsSecret)
            .OrderBy(command => command.Command, StringComparer.OrdinalIgnoreCase)
            .Select(command => new Dictionary<string, object>
            {
                ["name"] = command.Command, ["description"] = command.Description ?? "", ["cheat"] = command.IsCheat,
            }).ToList());
    }
}
