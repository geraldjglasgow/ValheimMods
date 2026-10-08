using System.Reflection;
using BepInEx;
using EliteCreaturesPack.Arsenal;
using EliteCraftingLink;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crafting;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Kraken;
using EliteCreaturesPack.Mimic;
using EliteCreaturesPack.RimeGiant;
using EliteCreaturesPack.Slinger;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace EliteCreaturesPack
{
    /// <summary>
    /// The plugin entry point. It binds the settings (synced from the server and lockable), applies every patch, has
    /// each creature build its prefabs whenever the game's network scene wakes, and starts the .cfg hot reload. The
    /// creatures live in their own folders: <c>Mimic</c>, <c>Slinger</c>, <c>RimeGiant</c>, <c>Kraken</c>,
    /// <c>Crossbow</c>, <c>Arsenal</c>, <c>Headsman</c>. The skeleton arsenal, every bone weapon, is built in
    /// <c>Arsenal</c> and, for the Bone Crossbow and the Executioner's Greataxe, beside their creatures (see
    /// <see cref="Arsenal.ArsenalItems"/>). Elite Crafting is optional: loaded first when present, it is told about the
    /// mod's gear and creatures (<see cref="CraftingHandOff"/>).
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(CraftingLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class EliteCreaturesPack : BaseUnityPlugin
    {
        public const string PluginGuid = "com.EliteCreaturesPack";
        public const string PluginName = "Elite Creatures Pack";
        public const string PluginVersion = "0.8.2";

        public static SyncedConfiguration Synced { get; private set; } = null!;

        private void Awake()
        {
            Log.Bind(Logger);
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            Settings.Initialize(Synced, this);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            InstallCreatures(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Elite Crafting, when installed: item classes and levels for the gear, rune and gear loot for the creatures.
            CraftingHandOff.Install();

            // Exceptions thrown by this mod's patches are logged under this mod's log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
            Logger.LogInfo($"{PluginName} {PluginVersion} ready.");
        }

        /// <summary>Each creature builds its prefabs whenever the game's network scene wakes.</summary>
        private static void InstallCreatures(Harmony harmony)
        {
            MimicPrefabs.Install(harmony);
            SlingerPrefabs.Install(harmony);
            RimeGiantPrefabs.Install(harmony);
            KrakenPrefabs.Install(harmony);
            // The arsenal builds first on each scene wake: the Bone Crossbow's and the greataxe's recipes take its spine.
            ArsenalPrefabs.Install(harmony);
            XbowPrefabs.Install(harmony);
            HeadsmanPrefabs.Install(harmony);
        }
    }
}
