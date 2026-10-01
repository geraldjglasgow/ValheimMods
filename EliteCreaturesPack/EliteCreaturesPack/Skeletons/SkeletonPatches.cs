using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Skeletons
{
    /// <summary>
    /// A wild skeleton's spawn may come as another skeleton (<see cref="SkeletonDraw.Wild"/>): the spawn is made again
    /// with its data, through the same method, so levels, hunting and despawning work as for the skeleton. When another
    /// mod's prefix has already skipped the spawn, this one lets it be. A failure is reported and the skeleton spawns as
    /// usual.
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    public static class SkeletonWildSpawnPatch
    {
        private static bool Prefix(SpawnSystem __instance, SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner, bool __runOriginal)
        {
            if (!__runOriginal)
            {
                return false;
            }
            SpawnSystem.SpawnData? instead = null;
            SafeCall.Run("SpawnSystem.Spawn skeleton draw", () => instead = SkeletonDraw.Wild(critter));
            if (instead == null)
            {
                return true;
            }
            __instance.Spawn(instead, spawnPoint, eventSpawner);
            return false;
        }
    }

    /// <summary>
    /// A fixed spawner (burial chambers, graves, ruins, cabins) may put another skeleton where its skeleton would stand
    /// (<see cref="SkeletonDraw.Instead"/>): its creature is swapped for this one spawn and put back afterwards,
    /// whatever happens, so the spawner itself never changes and keeps tracking what it spawned.
    /// </summary>
    [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
    public static class SkeletonCryptSpawnPatch
    {
        private static void Prefix(CreatureSpawner __instance, out GameObject? __state)
        {
            GameObject? swap = null;
            SafeCall.Run("CreatureSpawner.Spawn skeleton draw", () => swap = SkeletonDraw.Instead(__instance.m_creaturePrefab));
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

    /// <summary>A bone pile's pick may be another skeleton instead (<see cref="SkeletonDraw.BonePile"/>).</summary>
    [HarmonyPatch(typeof(SpawnArea), "SelectWeightedPrefab")]
    public static class SkeletonBonePileSpawnPatch
    {
        private static void Postfix(ref SpawnArea.SpawnData __result)
        {
            SpawnArea.SpawnData picked = __result;
            SafeCall.Run("SpawnArea.SelectWeightedPrefab skeleton draw", () => picked = SkeletonDraw.BonePile(picked));
            __result = picked;
        }
    }
}
