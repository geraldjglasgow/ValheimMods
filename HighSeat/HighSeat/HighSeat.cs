using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ConfigReload;
using HarmonyLib;
using PatchGuard;

namespace HighSeat
{
    /// <summary>
    /// The server runs every creature near any player: it loads every player's area, owns the creatures and spawners in
    /// it, and the game's own sync shows them to everyone. Server only: players need nothing installed.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class HighSeat : BaseUnityPlugin
    {
        public const string PluginGuid = "com.HighSeat";
        public const string PluginName = "HighSeat";
        public const string PluginVersion = "0.2.1";

        public static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            Settings.Bind(Config);
            ConfigReloader.Setup(Config, Logger);
            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }

        private void Update() => FrameGuard.Measure();
    }
}
