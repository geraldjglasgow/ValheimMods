using System.Reflection;
using BepInEx;
using EliteCreaturesPack.Arsenal;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Crossbow;
using EliteCreaturesPack.DraugrHound;
using EliteCreaturesPack.Haugbui;
using EliteCreaturesPack.Headsman;
using EliteCreaturesPack.Irrbloss;
using EliteCreaturesPack.Kraken;
using EliteCreaturesPack.LeechMatron;
using EliteCreaturesPack.Mimic;
using EliteCreaturesPack.Mountains;
using EliteCreaturesPack.RimeGiant;
using EliteCreaturesPack.Rootling;
using EliteCreaturesPack.Slinger;
using EliteCreaturesPack.Swamp;
using HarmonyLib;
using PatchGuard;
using SyncedConfig;

namespace EliteCreaturesPack
{
    /// <summary>
    /// The plugin entry point. It binds the settings (synced from the server and lockable), applies every patch, has
    /// each creature build its prefabs whenever the game's network scene wakes, and starts the .cfg hot reload. The
    /// creatures live in their own folders: <c>Mimic</c>, <c>Slinger</c>, <c>RimeGiant</c>, <c>Kraken</c>,
    /// <c>Crossbow</c>, <c>Arsenal</c>.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class EliteCreaturesPack : BaseUnityPlugin
    {
        public const string PluginGuid = "com.EliteCreaturesPack";
        public const string PluginName = "Elite Creatures Pack";
        public const string PluginVersion = "0.2.0";

        public static SyncedConfiguration Synced { get; private set; } = null!;

        private void Awake()
        {
            Log.Bind(Logger);
            Synced = new SyncedConfiguration(this, Logger, PluginName, PluginVersion);
            Settings.Initialize(Synced);

            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MimicPrefabs.Install(harmony);
            SlingerPrefabs.Install(harmony);
            RimeGiantPrefabs.Install(harmony);
            KrakenPrefabs.Install(harmony);
            XbowPrefabs.Install(harmony);
            ArsenalPrefabs.Install(harmony);
            MountainPrefabs.Install(harmony);
            SwampPrefabs.Install(harmony);
            HaugbuiPrefabs.Install(harmony);
            DraugrHoundPrefabs.Install(harmony);
            RootlingPrefabs.Install(harmony);
            LeechMatronPrefabs.Install(harmony);
            IrrblossPrefabs.Install(harmony);
            HeadsmanPrefabs.Install(harmony);

            // Writes the .cfg, hot reloads it on edit; Charter pushes reloaded values to clients.
            Synced.Finish(harmony);

            // Exceptions thrown by this mod's patches are logged under this mod's log source, then rethrown.
            Guard.Install(harmony, Logger, Assembly.GetExecutingAssembly());
            Logger.LogInfo($"{PluginName} {PluginVersion} ready.");
        }
    }
}
