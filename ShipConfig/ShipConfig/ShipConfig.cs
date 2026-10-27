using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace ShipConfig
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class ShipConfig : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ShipConfig";
        public const string PluginName = "ShipConfig";
        public const string PluginVersion = "1.6.0";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            ShipConfiguration.Initialize(Synced);
            ShipValueQueue.Initialize(this);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // The player-facing words, in the game's language (translations/English.yml and any translation files).
            Translations.Lang.Install(harmony, Assembly.GetExecutingAssembly(), "ShipConfig", Logger);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the ShipConfig log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
