using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Slinger
{
    /// <summary>
    /// A wild Greydwarf spawn may come as a slinger (<see cref="SlingerSpawns.Wild"/>): the spawn is made again with the
    /// slinger's data, through the same method, so levels, hunting and night despawn work as for the greydwarf. A
    /// failure is reported and the greydwarf spawns as usual.
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    public static class SlingerWildSpawnPatch
    {
        private static bool Prefix(SpawnSystem __instance, SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
        {
            SpawnSystem.SpawnData? slinger = null;
            SafeCall.Run("SpawnSystem.Spawn slinger", () => slinger = SlingerSpawns.Wild(critter, spawnPoint));
            if (slinger == null)
            {
                return true;
            }
            __instance.Spawn(slinger, spawnPoint, eventSpawner);
            return false;
        }
    }

    /// <summary>Each slingshot stone is aimed on its arc just before it leaves (<see cref="SlingerAim.Lob"/>).</summary>
    [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
    public static class SlingerAimPatch
    {
        private static void Prefix(Attack __instance)
        {
            if (__instance.m_attackOriginJoint == SlingerShot.Muzzle)   // every projectile passes here: bows, spells
            {
                SafeCall.Run("Attack.FireProjectileBurst slinger aim", () => SlingerAim.Lob(__instance));
            }
        }
    }

    /// <summary>A greydwarf nest's pick may be a slinger instead (<see cref="SlingerSpawns.Nest"/>).</summary>
    [HarmonyPatch(typeof(SpawnArea), "SelectWeightedPrefab")]
    public static class SlingerNestSpawnPatch
    {
        private static void Postfix(SpawnArea __instance, ref SpawnArea.SpawnData __result)
        {
            SpawnArea.SpawnData picked = __result;
            SafeCall.Run("SpawnArea.SelectWeightedPrefab slinger", () => picked = SlingerSpawns.Nest(picked, __instance.transform.position));
            __result = picked;
        }
    }
}
