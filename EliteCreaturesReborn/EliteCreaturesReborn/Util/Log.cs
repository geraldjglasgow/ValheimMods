using BepInEx.Logging;

namespace EliteCreaturesReborn.Util
{
    /// <summary>Thin static wrapper over the plugin's logger so any class can log without holding a reference.</summary>
    internal static class Log
    {
        private static ManualLogSource? _log;

        /// <summary>
        /// When true, the step-by-step diagnostics (<see cref="Diag"/>) are emitted. Off by default so a live server is
        /// quiet; a server owner turns it on from the .cfg to make a mutation that "silently does nothing" - the class of
        /// bug that hid the Splintering ZDO-reset fault - visible at every step instead. Bound once from the config.
        /// </summary>
        public static bool Diagnostics;

        public static void Bind(ManualLogSource log) => _log = log;

        public static void Info(string message) => _log?.LogInfo(message);

        public static void Warn(string message) => _log?.LogWarning(message);

        public static void Error(string message) => _log?.LogError(message);

        /// <summary>A permanent diagnostic line, printed only while <see cref="Diagnostics"/> is on. Never removed.</summary>
        public static void Diag(string message)
        {
            if (Diagnostics)
            {
                _log?.LogInfo("[diag] " + message);
            }
        }
    }
}
