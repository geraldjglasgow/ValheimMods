using HarmonyLib;
using SyncedConfig;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Entry point of the Terrain module: the edit pipeline (dispatcher, owner handler, server relay, refusals) and the
    /// engine's settings (section 3 operations tuning and section 6 height limits, bound by <see cref="EngineSettings"/>).
    /// </summary>
    public static class TerrainModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            Refusals.Initialize();
            EngineSettings.Bind(synced);
        }

        internal static void RegisterRpcs()
        {
            ServerRelay.EnsureRegistered();
            Refusals.EnsureRegistered();
        }
    }

    /// <summary>Routed RPCs are registered on every machine when a session starts, next to the game's own.</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class GameStartRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => TerrainModule.RegisterRpcs();
    }
}
