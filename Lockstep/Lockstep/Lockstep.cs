using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace Lockstep
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Lockstep : BaseUnityPlugin
    {
        public const string PluginGuid = "com.Lockstep";
        public const string PluginName = "Lockstep";
        public const string PluginVersion = "0.3.0";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            LockstepConfiguration.Initialize(Synced);
            ProgressState.Initialize(Synced);
            Chain.Changed += ProgressServer.Publish;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Writes the .cfg, hot reloads it on edit, installs the YAML hooks; Charter pushes values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the Lockstep log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
