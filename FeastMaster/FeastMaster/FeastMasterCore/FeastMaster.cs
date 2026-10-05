using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ItemCopies;
using PatchGuard;
using SyncedConfig;

namespace FeastMaster
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class FeastMaster : BaseUnityPlugin
    {
        public const string PluginGuid = "com.FeastMaster";
        public const string PluginName = "FeastMaster";
        public const string PluginVersion = "4.7.3";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            FeastMasterData.Initialize(Synced);
            Settings.Initialize(Synced);
            ItemValues.HookConfig(Config);
            WorldRates.HookConfig(Config);
            Config.SettingChanged += (_, __) => PatchSwitch.MarkDirty();
            Config.ConfigReloaded += (_, __) => PatchSwitch.MarkDirty();

            // Each patch class is installed only while a setting it serves is changed, see PatchSwitch.
            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchSwitch.Initialize(harmony, Assembly.GetExecutingAssembly());
            // New world items and items entering an inventory carry their own shared data copy; write the values into it.
            Copies.HookSpawns(harmony, ItemValues.ApplyCopy);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);
            Log.LogInfo($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the FeastMaster log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
