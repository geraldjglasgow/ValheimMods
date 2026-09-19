using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using OpenKeep.Capacity;
using OpenKeep.Carts;
using OpenKeep.Core;
using OpenKeep.Reach;
using OpenKeep.Salvage;
using OpenKeep.Shared;
using OpenKeep.Signs;
using OpenKeep.Stacks;
using OpenKeep.Stow;
using PatchGuard;
using SyncedConfig;

namespace OpenKeep
{
    /// <summary>
    /// Plugin entry. Every module has an Initialize(SyncedConfiguration) that binds its settings, registers its
    /// YAML files and its language words; patches are applied per class afterwards, Guard.Install goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.openkeep";
        public const string PluginName = "OpenKeep";
        public const string PluginVersion = "1.1.1";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }
        public static ConfigEntry<bool> LockConfiguration { get; private set; }
        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            LockConfiguration = Synced.BindLocking("General", "Lock Configuration", true,
                "[Server Only] Clients cannot change synced settings while connected to a server that has this on.");
            InitializeModules();

            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchEverything(harmony);
            StacksModule.HookSpawns(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);
            Log.LogInfo($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the OpenKeep log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }

        /// <summary>Every module binds its settings, registers its YAML files and its words. Order is the spec's.</summary>
        private static void InitializeModules()
        {
            CoreModule.Initialize(Synced);
            ReachModule.Initialize(Synced);
            StowModule.Initialize(Synced);
            SalvageModule.Initialize(Synced);
            StacksModule.Initialize(Synced);
            CapacityModule.Initialize(Synced);
            CartsModule.Initialize(Synced);
            SignsModule.Initialize(Synced);
            SharedModule.Initialize(Synced);
        }

        private void Update() => Synced.YamlEditor.Update();

        private void OnGUI() => Synced.YamlEditor.OnGUI();

        /// <summary>Applies every patch class on its own so one failure is logged and the others still apply.</summary>
        private static int PatchEverything(Harmony harmony)
        {
            int failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    failed++;
                    Log.LogError($"Patch {type.FullName} failed to apply: {e.Message}");
                }
            }
            return failed;
        }
    }
}
