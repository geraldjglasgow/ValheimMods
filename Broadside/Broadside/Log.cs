using BepInEx.Logging;

namespace Broadside
{
    /// <summary>Thin static wrapper over the plugin's logger so any class can log without holding a reference.</summary>
    internal static class Log
    {
        private static ManualLogSource? log;

        public static void Bind(ManualLogSource source) => log = source;

        public static void Info(string message) => log?.LogInfo(message);

        public static void Warn(string message) => log?.LogWarning(message);

        public static void Error(string message) => log?.LogError(message);
    }
}
