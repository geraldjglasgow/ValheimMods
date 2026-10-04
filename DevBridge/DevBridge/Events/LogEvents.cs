using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace DevBridge.Events
{
    /// <summary>log events: BepInEx warnings and errors (Unity's log included), from any thread. At most PerSecond a
    /// second, so a mod warning every frame cannot push everything else out of the ring; the next one says how many
    /// were dropped.</summary>
    internal sealed class LogEvents : ILogListener
    {
        private const int PerSecond = 20;
        private const int MaxText = 2000;
        private const LogLevel Wanted = LogLevel.Fatal | LogLevel.Error | LogLevel.Warning;

        private readonly object gate = new object();
        private long second;
        private int used;
        private int dropped;

        internal static void Install() => Logger.Listeners.Add(new LogEvents());

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            try
            {
                if ((eventArgs.Level & Wanted) == 0 || !Admit(out int skipped)) return;
                string text = eventArgs.Data?.ToString() ?? "";
                var data = new Dictionary<string, object>
                {
                    ["level"] = eventArgs.Level.ToString(),
                    ["source"] = eventArgs.Source?.SourceName,
                    ["text"] = text.Length <= MaxText ? text : text.Substring(0, MaxText) + "...",
                };
                if (skipped > 0) data["dropped"] = skipped;
                EventLog.Add("log", data);
            }
            catch (Exception)
            {
                // logging the failure would come straight back here
            }
        }

        private bool Admit(out int skipped)
        {
            lock (gate)
            {
                long now = Stopwatch.GetTimestamp() / Stopwatch.Frequency;
                if (now != second) (second, used) = (now, 0);
                skipped = 0;
                if (used >= PerSecond)
                {
                    dropped++;
                    return false;
                }
                used++;
                (skipped, dropped) = (dropped, 0);
                return true;
            }
        }

        public void Dispose()
        {
        }
    }

    /// <summary>console events: every line the console and chat print, without rich-text tags (beside ConsoleCapture,
    /// which keeps the same lines for /console and /log?buffer=console).</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), typeof(string))]
    internal static class ConsoleEvents
    {
        private static readonly Regex Tags = new Regex("<[^>]*>");

        private static void Postfix(Terminal __instance, string text) => Publish.Safely("console", () =>
        {
            string plain = Tags.Replace(text ?? "", "").Trim();
            if (plain.Length == 0) return;
            EventLog.Add("console", new Dictionary<string, object> { ["source"] = __instance is Chat ? "chat" : "console", ["text"] = plain });
        });
    }
}
