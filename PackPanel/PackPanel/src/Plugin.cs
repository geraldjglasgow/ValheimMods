using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EliteCraftingLink;
using HarmonyLib;
using PatchGuard;
using PackPanel.Backpacks;
using PackPanel.Core;
using PackPanel.Elite;
using PackPanel.Look;
using PackPanel.Tackle;
using SyncedConfig;

namespace PackPanel
{
    /// <summary>
    /// Plugin entry. <see cref="InventoryModule"/> binds the settings, registers the backpacks' and tackleboxes' YAML
    /// files and the words; with EliteCrafting present (loaded first, soft dependency) the backpacks are registered with
    /// it (<see cref="EliteSetup"/>); patches are applied per class afterwards, the backpacks' and tackleboxes' prefabs
    /// hook ZNetScene, Guard.Install goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Layout.BiomeLordsLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(CraftingLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.packpanel";
        public const string PluginName = "PackPanel";
        public const string PluginVersion = "0.14.0";

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
            InventoryModule.Initialize(Synced);
            EliteSetup.Register();

            Harmony harmony = new Harmony(PluginGuid);
            int failed = PatchEverything(harmony);
            BackpackPrefab.Install(harmony);
            TackleboxPrefab.Install(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);
            Log.LogInfo($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the PackPanel log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
        }

        private void Update()
        {
            GamePanelTheme.Update();
            NightShade.Update();
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
