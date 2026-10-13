using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Horde
{
    /// <summary>
    /// The horde's hold on the game's world spawning, which runs once a second on the client that owns each zone with a
    /// player in it. <c>UpdateSpawnList</c>: while the horde is on, the list's groups are grown for the pass and put back
    /// after (<see cref="HordeGroups"/>); an event's list (the game's raids) is left alone. <c>Spawn</c>: every creature it
    /// makes outside an event is tagged wild (<see cref="WildSpawn"/>), horde on or off, so the horde can weaken it. Both
    /// finalizers run whatever happens, so nothing grown or flagged outlives the call.
    /// </summary>
    [HarmonyPatch]
    internal static class HordeSpawnPatch
    {
        [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawnList")]
        [HarmonyPrefix]
        private static void BeforeList(List<SpawnSystem.SpawnData> spawners, bool eventSpawners)
        {
            if (!eventSpawners && spawners != null && HordeSettings.Enabled && HordeSettings.Spawns > 1f)
            {
                SafeCall.Run("SpawnSystem.UpdateSpawnList horde", static (list, factor) => HordeGroups.Grow(list, factor),
                    spawners, HordeSettings.Spawns);
            }
        }

        [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawnList")]
        [HarmonyFinalizer]
        private static Exception? AfterList(Exception? __exception)
        {
            SafeCall.Run("SpawnSystem.UpdateSpawnList restore", static () => HordeGroups.Restore());
            return __exception;
        }

        [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
        [HarmonyPrefix]
        private static void BeforeSpawn(bool eventSpawner, out bool __state) => __state = WildSpawn.Begin(!eventSpawner);

        [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
        [HarmonyFinalizer]
        private static Exception? AfterSpawn(Exception? __exception, bool __state)
        {
            WildSpawn.End(__state);
            return __exception;
        }
    }
}
