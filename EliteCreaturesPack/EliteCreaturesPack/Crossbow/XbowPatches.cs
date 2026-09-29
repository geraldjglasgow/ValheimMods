using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// A wild archer skeleton's spawn may come as a crossbowman (<see cref="XbowSpawns.Wild"/>): the spawn is made again with the
    /// crossbowman's data, through the same method, so levels, hunting and despawning work as for the skeleton. When
    /// another prefix (the arsenal skeletons') has already spawned something in the skeleton's place, this one lets it
    /// be. A failure is reported and the skeleton spawns as usual.
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    public static class XbowWildSpawnPatch
    {
        private static bool Prefix(SpawnSystem __instance, SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner, bool __runOriginal)
        {
            if (!__runOriginal)
            {
                return false;
            }
            SpawnSystem.SpawnData? crossbowman = null;
            SafeCall.Run("SpawnSystem.Spawn crossbowman", () => crossbowman = XbowSpawns.Wild(critter));
            if (crossbowman == null)
            {
                return true;
            }
            __instance.Spawn(crossbowman, spawnPoint, eventSpawner);
            return false;
        }
    }

    /// <summary>
    /// A fixed spawner (burial chambers, graves, ruins, cabins) may put a crossbowman where its archer skeleton would
    /// stand (<see cref="XbowSpawns.Instead"/>): its creature is swapped for this one spawn and put back afterwards, whatever
    /// happens, so the spawner itself never changes and keeps tracking what it spawned.
    /// </summary>
    [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
    public static class XbowCryptSpawnPatch
    {
        private static void Prefix(CreatureSpawner __instance, out GameObject? __state)
        {
            GameObject? swap = null;
            SafeCall.Run("CreatureSpawner.Spawn crossbowman", () => swap = XbowSpawns.Instead(__instance.m_creaturePrefab));
            __state = swap == null ? null : __instance.m_creaturePrefab;
            if (swap != null)
            {
                __instance.m_creaturePrefab = swap;
            }
        }

        private static void Finalizer(CreatureSpawner __instance, GameObject? __state)
        {
            if (__state != null)
            {
                __instance.m_creaturePrefab = __state;
            }
        }
    }

    /// <summary>A bone pile's pick may be a crossbowman instead (<see cref="XbowSpawns.BonePile"/>).</summary>
    [HarmonyPatch(typeof(SpawnArea), "SelectWeightedPrefab")]
    public static class XbowBonePileSpawnPatch
    {
        private static void Postfix(ref SpawnArea.SpawnData __result)
        {
            SpawnArea.SpawnData picked = __result;
            SafeCall.Run("SpawnArea.SelectWeightedPrefab crossbowman", () => picked = XbowSpawns.BonePile(picked));
            __result = picked;
        }
    }
}
