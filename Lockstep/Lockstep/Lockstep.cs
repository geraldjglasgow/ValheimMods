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
        public const string PluginVersion = "0.6.0";

        public static Lockstep Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            LockstepConfiguration.Initialize(Synced);
            ProgressState.Initialize(Synced);
            ServerCheck.Initialize(Synced.Sync);
            Chain.Changed += ProgressServer.Publish;
            Chain.Changed += KillWitness.ChainChanged;

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // The player-facing words, in the game's language (translations/English.yml and any translation files).
            Translations.Lang.Install(harmony, Assembly.GetExecutingAssembly(), "Lockstep", Logger);

            // Writes the .cfg, hot reloads it on edit, installs the YAML hooks; Charter pushes values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under the Lockstep log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
