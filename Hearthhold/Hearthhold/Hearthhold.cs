using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace Hearthhold
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(GrindstoneLink.Guid, BepInDependency.DependencyFlags.HardDependency)]
    public class Hearthhold : BaseUnityPlugin
    {
        public const string PluginGuid = "com.Hearthhold";
        public const string PluginName = "Hearthhold";
        public const string PluginVersion = "0.1.0";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            Settings.Initialize(Synced);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the Hearthhold log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }
    }
}
