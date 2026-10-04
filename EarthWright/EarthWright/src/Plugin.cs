using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Clearing;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Extras;
using EarthWright.Gear;
using EarthWright.History;
using EarthWright.Menu;
using EarthWright.Paths;
using EarthWright.Preview;
using EarthWright.Protection;
using EarthWright.Terrain;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace EarthWright
{
    /// <summary>
    /// Plugin entry. Every module has an Initialize(SyncedConfiguration) that binds its settings, registers its YAML
    /// files, words, console subcommands, guards and event handlers; patches are applied per class afterwards, and
    /// Guard.Install goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.earthwright";
        public const string PluginName = "EarthWright";
        public const string PluginVersion = "0.3.2";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }
        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            InitializeModules();
            WireModules();

            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchEverything(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);
            Log.LogInfo($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the EarthWright log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }

        /// <summary>Every module binds its settings and registers its words and hooks, in section order.</summary>
        private static void InitializeModules()
        {
            CoreModule.Initialize(Synced);
            TerrainModule.Initialize(Synced);
            ActionsModule.Initialize(Synced);
            BrushModule.Initialize(Synced);
            PreviewModule.Initialize(Synced);
            PathsModule.Initialize(Synced);
            HistoryModule.Initialize(Synced);
            // Protection before Costs: its guards run first, so a ward or lock refusal shows instead of a cost message.
            ProtectionModule.Initialize(Synced);
            CostsModule.Initialize(Synced);
            ClearingModule.Initialize(Synced);
            MenuModule.Initialize(Synced);
            GearModule.Initialize(Synced);
            ExtrasModule.Initialize(Synced);
        }

        /// <summary>
        /// Connects modules that were written against each other's interfaces only: a module never reads another
        /// module's settings directly, the plugin hands the value over here.
        /// </summary>
        private static void WireModules()
        {
            MenuHooks.ClearingEnabled = () => ClearingSettings.Enabled;
        }

        private void Update()
        {
            Synced.YamlEditor.Update();
            Ticker.Tick();
        }

        private void OnGUI()
        {
            Synced.YamlEditor.OnGUI();
            Ticker.Gui();
        }

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
