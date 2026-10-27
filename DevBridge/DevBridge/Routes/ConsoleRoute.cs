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
    /// <summary>/console runs game console commands and returns what they printed; /log reads the recent log.</summary>
    internal static class ConsoleRoute
    {
        internal static void Register(Router router) => router.Add("/console",
            "/console?cmd=<command>&wait=0.3\n" +
            "                       run a console command as if typed (F5), reply with what the console printed within wait seconds;\n" +
            "                       several, one per line (POST them), run in order a frame apart, each answer under \"> command\";\n" +
            "                       cheats need cmd=devcommands first (and a server you are admin on)",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            string commands = request.Get("cmd") ?? request.Get("body") ?? throw new BridgeException("give cmd=<console command>");
            ConsoleRuns.Start(request, commands, ran => request.Text(Text(ran)));
        }

        private static string Text(List<ConsoleRuns.Ran> ran) => ran.Count == 1 ? Printed(ran[0])
            : string.Join("\n", ran.Select(one => $"> {one.Command}\n{Printed(one)}"));

        private static string Printed(ConsoleRuns.Ran ran) =>
            ran.Printed.Count == 0 ? "(the console printed nothing)" : string.Join("\n", ran.Printed);
    }

    /// <summary>
    /// Console commands run as if typed, one a line, in order and a frame apart, then what the console printed after
    /// each, read wait seconds after the last (a line printed later counts for the last command). Used by /console and
    /// the studio's Commands tab.
    /// </summary>
    internal static class ConsoleRuns
    {
        internal sealed class Ran
        {
            internal string Command;
            internal List<string> Printed;
        }

        internal static void Start(BridgeRequest request, string text, Action<List<Ran>> answer)
        {
            string[] commands = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
            if (commands.Length == 0) throw new BridgeException("no command to run");
            if (!global::Console.instance) throw new BridgeException("the console does not exist yet");
            Async.Start(request, Run(request, commands, answer));
        }

        private static IEnumerator Run(BridgeRequest request, string[] commands, Action<List<Ran>> answer)
        {
            var marks = new long[commands.Length + 1];
            for (int i = 0; i < commands.Length; i++)
            {
                if (i > 0) yield return null;
                marks[i] = ConsoleCapture.Lines.Next;
                if (!global::Console.instance) throw new BridgeException($"the console went away before {commands[i]}");
                global::Console.instance.TryRunCommand(commands[i], silentFail: false, skipAllowedCheck: true);
            }
            yield return new WaitForSecondsRealtime(request.Float("wait", 0.3f));
            marks[commands.Length] = long.MaxValue;
            List<LineBuffer.Line> printed = ConsoleCapture.Lines.Since(marks[0], 500, _ => true);
            answer(commands.Select((command, i) => new Ran
            {
                Command = command,
                Printed = printed.Where(line => line.Seq >= marks[i] && line.Seq < marks[i + 1]).Select(line => line.Text).ToList(),
            }).ToList());
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
