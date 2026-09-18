using BepInEx.Configuration;
using BepInEx.Logging;
using HaloMenu.Config;

namespace HaloMenu
{
    /// <summary>Wraps the plugin's ManualLogSource, filtered by the Debug/LogLevel setting.</summary>
    public static class HaloLog
    {
        public static ManualLogSource Source;
        public static ConfigEntry<HaloLogLevel> Level;

        public static void Debug(string message)
        {
            if (Level != null && Level.Value >= HaloLogLevel.Debug)
                Source?.LogInfo(message);
        }

        public static void Info(string message)
        {
            if (Level != null && Level.Value >= HaloLogLevel.Info)
                Source?.LogInfo(message);
        }

        public static void Warning(string message)
        {
            if (Level != null && Level.Value >= HaloLogLevel.Warning)
                Source?.LogWarning(message);
        }

        public static void Error(string message) => Source?.LogError(message);
    }
}
