using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using DevBridge.Logs;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/console runs a game console command and returns what it printed; /log reads the recent log.</summary>
    internal static class ConsoleRoute
    {
        internal static void Register(Router router) => router.Add("/console",
            "/console?cmd=<command>&wait=0.3\n" +
            "                       run a console command as if typed (F5), reply with what the console printed within wait seconds;\n" +
            "                       cheats need cmd=devcommands first (and a server you are admin on)",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            string command = request.Get("cmd") ?? request.Get("body") ?? throw new BridgeException("give cmd=<console command>");
            global::Console console = global::Console.instance;
            if (!console) throw new BridgeException("the console does not exist yet");
            long mark = ConsoleCapture.Lines.Next;
            console.TryRunCommand(command.Trim(), silentFail: false, skipAllowedCheck: true);
            Async.Start(request, Collect(request, mark));
        }

        private static IEnumerator Collect(BridgeRequest request, long mark)
        {
            yield return new WaitForSecondsRealtime(request.Float("wait", 0.3f));
            List<LineBuffer.Line> printed = ConsoleCapture.Lines.Since(mark, 500, _ => true);
            request.Text(printed.Count == 0 ? "(the console printed nothing)" : string.Join("\n", printed.Select(l => l.Text)));
        }
    }

    internal static class LogRoute
    {
        internal static void Register(Router router) => router.Add("/log",
            "/log?since=N&level=warning&grep=text&limit=100&buffer=log|console\n" +
            "                       numbered recent lines of the BepInEx log (Unity's log included) or of the console;\n" +
            "                       the last line gives next=N, pass it as since= to read only newer lines",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            LineBuffer buffer = request.Get("buffer") == "console" ? ConsoleCapture.Lines : LogCapture.Lines;
            LogLevel worst = Level(request.Get("level"));
            string grep = request.Get("grep");
            long since = long.TryParse(request.Get("since"), out long parsed) ? parsed : 0;
            List<LineBuffer.Line> lines = buffer.Since(since, request.Int("limit", 100), l => Keep(l, worst, grep));
            request.Text(string.Join("\n", lines.Select(l => $"{l.Seq} {l.Text}").Concat(new[] { $"next={buffer.Next}" })));
        }

        private static bool Keep(LineBuffer.Line line, LogLevel worst, string grep)
        {
            if (worst != LogLevel.None && (line.Level == LogLevel.None || (int)line.Level > (int)worst)) return false;
            return grep == null || line.Text.IndexOf(grep, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static LogLevel Level(string name)
        {
            if (name == null) return LogLevel.None;
            if (Enum.TryParse(name, true, out LogLevel level)) return level;
            throw new BridgeException("level= takes fatal, error, warning, message, info or debug");
        }
    }
}
