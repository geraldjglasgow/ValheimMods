using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A spawn point round a lone player, checked against what the server left out there (<see cref="LoneLoad"/>). The
    /// game turns down a spot with anything solid over it (ZoneSystem.IsBlocked: a ray down onto buildings, trees and
    /// rocks), and the server has not made the buildings, trees and rocks of a lone player's sectors, so a spot the game
    /// passed is turned down here too when it lies within the footprint of one of them (a circle round the object
    /// covering its solid colliders: a little stricter than the ray, never looser). Two of the game's spawn tests:
    /// SpawnSystem.IsSpawnPointGood (wild spawns and raids; the game then tries other spots) and SpawnArea.FindSpawnPoint
    /// for a nest that spawns on open ground only. Only on the server while it runs creatures, and only once the game's
    /// own answer was yes; the spot's sector and its eight neighbours are asked, since a
    /// footprint can cross a sector's edge. Spawn points (CreatureSpawner, TriggerSpawner) stand where the world put
    /// them, and a boss altar's ground is loaded in full (<see cref="FullSectors"/>).
    /// </summary>
    [HarmonyPatch]
    internal static class SpawnPoints
    {
        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood))]
        [HarmonyPostfix]
        private static void WildSpot(ref bool __result, ref Vector3 spawnPoint)
        {
            if (__result && ServerRun.Active && Blocked(spawnPoint))
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.FindSpawnPoint))]
        [HarmonyPostfix]
        private static void NestSpot(SpawnArea __instance, ref bool __result, ref Vector3 point)
        {
            if (__result && __instance.m_onGroundOnly && ServerRun.Active && Blocked(point))
            {
                __result = false;
            }
        }

        private static bool Blocked(Vector3 point)
        {
            Vector2s zone = ZoneSystem.GetZone(point);
            float now = Time.time;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    uint index = ZoneSystem.SectorToIndex(zone.x + dx, zone.y + dy).Sector;
                    if (index != 0 && LoneSectors.Blocks(index, point, now))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
