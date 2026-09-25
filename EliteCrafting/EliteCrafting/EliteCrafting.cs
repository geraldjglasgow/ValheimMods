using System;
using System.Reflection;
using BepInEx;
using ConfigReload;
using EliteCrafting.Config;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using EliteCrafting.Text;
using HarmonyLib;
using PatchGuard;

namespace EliteCrafting
{
    /// <summary>
    /// The plugin entry point. Awake runs, in order: the Charter (server binding), the rules (both YAML families: load,
    /// validate, sync, hot reload), the .cfg settings, the English words, then one <c>Init()</c> per feature area. Then
    /// every [HarmonyPatch] class in the assembly is applied (one class at a time), Charter installs its hooks, and ConfigReload starts
    /// watching the .cfg. Feature areas never edit this file: they fill their own <c>*Feature.Init</c> and add patch
    /// classes in their own folders.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class EliteCraftingPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.EliteCrafting";
        public const string PluginName = "EliteCrafting";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Log.Bind(Logger);
            ServerBinding.Create(Config, PluginGuid, PluginVersion);
            ActiveRules.Initialise(ServerBinding.Charter);
            ModSettings.Bind(Config);
            Words.Initialise();
            InitFeatures();
            Harmony harmony = new Harmony(PluginGuid);
            PatchEach(harmony);
            Charter.Charter.Install(harmony);
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
            Words.InstallNow();
            ConfigReloader.Setup(Config, Logger);
            Logger.LogInfo($"{PluginName} {PluginVersion} ready.");
        }

        private static void InitFeatures()
        {
            InitArea("ItemsFeature.Init", Items.ItemsFeature.Init);
            InitArea("StonesFeature.Init", Stones.StonesFeature.Init);
            InitArea("EffectsFeature.Init", Effects.EffectsFeature.Init);
            InitArea("DisplayFeature.Init", Display.DisplayFeature.Init);
            InitArea("LootFeature.Init", Loot.LootFeature.Init);
            InitArea("CommandsFeature.Init", Commands.CommandsFeature.Init);
            InitArea("SalvageFeature.Init", Salvage.SalvageFeature.Init);
        }

        // One area failing to start must not stop the rest of Awake: without the patches the stone prefabs are never
        // registered, and every stone in a loaded inventory would be dropped as an unknown prefab. Guard logs it.
        private static void InitArea(string name, Action init)
        {
            try
            {
                Guard.Run(name, init);
            }
            catch (Exception)
            {
                // already logged under the mod's name by Guard.Run
            }
        }

        // PatchAll stops at the first patch class that fails (a game update renaming a method), leaving every later
        // class unapplied. Patched one class at a time, a failure disables that feature only, with an error naming it.
        private static void PatchEach(Harmony harmony)
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                try
                {
                    if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                    {
                        harmony.CreateClassProcessor(type).Patch();
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"patch {type.FullName} failed to apply, its feature is off: {e}");
                }
            }
        }
    }
}
