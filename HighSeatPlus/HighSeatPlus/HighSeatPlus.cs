using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ConfigReload;
using HarmonyLib;
using PatchGuard;

namespace HighSeatPlus
{
    /// <summary>
    /// The players' half of HighSeat for big servers: big bases and crowds made cheap to draw and run on a player's PC.
    /// Optional: a player without it plays as today. Nothing written yet; the design is in PLAN.md.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class HighSeatPlus : BaseUnityPlugin
    {
        public const string PluginGuid = "com.HighSeatPlus";
        public const string PluginName = "HighSeatPlus";
        public const string PluginVersion = "0.1.0";

        public static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;
            ConfigReloader.Setup(Config, Logger);
            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
