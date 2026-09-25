using BepInEx.Logging;

namespace EliteCrafting.Core
{
    /// <summary>Thin static wrapper over the plugin's logger so any class can log without holding a reference.</summary>
    public static class Log
    {
        private static ManualLogSource? _log;

        public static void Bind(ManualLogSource log) => _log = log;

        public static void Info(string message) => _log?.LogInfo(message);

        public static void Warn(string message) => _log?.LogWarning(message);

        public static void Error(string message) => _log?.LogError(message);

        public static void Debug(string message) => _log?.LogDebug(message);
    }
}
