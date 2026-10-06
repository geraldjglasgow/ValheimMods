using System.Text.RegularExpressions;
using BepInEx.Logging;
using DevBridge.Events;
using HarmonyLib;

namespace DevBridge.Logs
{
    /// <summary>Keeps the recent BepInEx log (which carries Unity's own log too) for /log and /wait.</summary>
    internal sealed class LogCapture : ILogListener
    {
        internal static readonly LineBuffer Lines = new LineBuffer(5000, showLevel: true);

        internal static void Install() => Logger.Listeners.Add(new LogCapture());

        public void LogEvent(object sender, LogEventArgs eventArgs) =>
            Lines.Add(eventArgs.Level, eventArgs.Source?.SourceName, Body(eventArgs.Data));

        // A string is kept as it came. Anything else becomes its text now, on the thread that logged it: a Unity
        // object's name can be read only on the main thread, and the object may change or go before the line is read.
        private static string Body(object data) => data as string ?? data?.ToString();

        public void Dispose()
        {
        }
    }

    /// <summary>Everything the console and chat print, without rich-text tags, for /console and /log?buffer=console, and
    /// the console events: the tags are taken out once here and the plain text handed to both.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), typeof(string))]
    internal static class ConsoleCapture
    {
        internal static readonly LineBuffer Lines = new LineBuffer(1000, showLevel: false);

        private static readonly Regex Tags = new Regex("<[^>]*>");

        private static void Postfix(Terminal __instance, string text)
        {
            string source = __instance is Chat ? "chat" : "console";
            string plain = Plain(text);
            Lines.Add(LogLevel.Message, source, plain);
            ConsoleEvents.Printed(source, plain);
        }

        // Most lines carry no tag, and the regex runs only on those that do.
        private static string Plain(string text) =>
            string.IsNullOrEmpty(text) ? "" : text.IndexOf('<') < 0 ? text : Tags.Replace(text, "");
    }
}
