using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Core
{
    /// <summary>
    /// A spawn area (bone pile, greydwarf nest) counts the pack's stand-ins as the creatures they replaced, untamed only
    /// as the game counts its own, so its near and total caps hold (<see cref="StandIns.CountsFor"/>). Runs on the
    /// area's owner, for each creature it counts before a spawn.
    /// </summary>
    [HarmonyPatch(typeof(SpawnArea), "IsSpawnPrefab")]
    public static class StandInSpawnAreaPatch
    {
        private static void Postfix(SpawnArea __instance, GameObject go, ref bool __result)
        {
            if (!__result)
            {
                __result = SafeCall.Run("SpawnArea.IsSpawnPrefab stand-ins",
                    static (area, creature) => StandIns.CountsFor(area, creature), __instance, go, false);
            }
        }
    }

    /// <summary>A wild spawn's cap counts the pack's stand-ins as the creature they replaced (<see cref="StandIns.Count"/>).</summary>
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfZDOInstances))]
    public static class StandInWildCountPatch
    {
        private static void Postfix(GameObject prefab, List<ZDO> ZDOs, bool eventCreaturesOnly, ref int __result)
        {
            __result += SafeCall.Run("SpawnSystem.GetNrOfZDOInstances stand-ins",
                static (game, zdos, events) => StandIns.Count(game, zdos, events), prefab, ZDOs, eventCreaturesOnly, 0);
        }
    }
}
