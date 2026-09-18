using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;
using Party.Client;

namespace Party
{
    [BepInPlugin(PluginInfo.PluginGuid, PluginInfo.PluginName, PluginInfo.PluginVersion)]
    public class PartyPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = PluginInfo.PluginGuid;
        public const string PluginName = PluginInfo.PluginName;
        public const string PluginVersion = PluginInfo.PluginVersion;

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            PartyConfig.Initialize(Synced);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // RPCs register on ZNet.Awake (ZRoutedRpc.instance does not exist yet here) and console
            // commands register on Terminal.InitTerminal - see Hooks/NetHooks.cs and Commands/PartyCommands.cs.

            gameObject.AddComponent<PartyTicker>();

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the Party log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
