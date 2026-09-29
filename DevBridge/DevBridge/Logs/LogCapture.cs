using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace DevBridge.Logs
{
    /// <summary>Keeps the recent BepInEx log (which carries Unity's own log too) for /log and /wait.</summary>
    internal sealed class LogCapture : ILogListener
    {
        internal static readonly LineBuffer Lines = new LineBuffer(5000);

        internal static void Install() => Logger.Listeners.Add(new LogCapture());

        public void LogEvent(object sender, LogEventArgs eventArgs) =>
            Lines.Add($"{eventArgs.Level} {eventArgs.Source.SourceName}: {eventArgs.Data}", eventArgs.Level);

        public void Dispose()
        {
        }
    }

    /// <summary>Everything the console and chat print, without rich-text tags, for /console and /log?buffer=console.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), typeof(string))]
    internal static class ConsoleCapture
    {
        internal static readonly LineBuffer Lines = new LineBuffer(1000);

        private static readonly Regex Tags = new Regex("<[^>]*>");

        private static void Postfix(Terminal __instance, string text)
        {
            string source = __instance is Chat ? "chat" : "console";
            Lines.Add(source + ": " + Tags.Replace(text ?? "", ""), LogLevel.Message);
        }
    }
}
