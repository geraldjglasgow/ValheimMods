using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EliteEquipment.Boots;
using EliteEquipment.Casters;
using EliteEquipment.Shields;
using EliteEquipment.Weapons;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace EliteEquipment
{
    /// <summary>
    /// Plugin entry. The words (translations/English.yml) are installed first; every feature has an
    /// Initialize(SyncedConfiguration) that binds its settings; patches are applied per class afterwards, Guard.Install
    /// goes last.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(EliteCraftingLink.CraftingLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "milkyteam.eliteequipment";
        public const string PluginName = "EliteEquipment";
        public const string PluginVersion = "0.4.1";

        public static ManualLogSource Log { get; private set; }
        public static SyncedConfiguration Synced { get; private set; }
        public static ConfigEntry<bool> LockConfiguration { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            LockConfiguration = Synced.BindLocking("General", "Lock Configuration", true,
                "[Server Only] Clients cannot change synced settings while connected to a server that has this on.");

            Harmony harmony = new Harmony(PluginGuid);
            // The player-facing words, in the game's language (translations/English.yml and any translation files).
            Translations.Lang.Install(harmony, Assembly.GetExecutingAssembly(), "EliteEquipment", Logger);
            BootsModule.Initialize(Synced);
            ShieldsModule.Initialize(Synced);
            WeaponsModule.Initialize(Synced);
            CastersModule.Initialize(Synced);
            int failed = PatchEverything(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);
            Log.LogInfo($"Loading [{PluginName} {PluginVersion}]" + (failed > 0 ? $" with {failed} failed patches, see above" : ""));

            // Exceptions thrown by this mod's patches are logged under the EliteEquipment log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
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
