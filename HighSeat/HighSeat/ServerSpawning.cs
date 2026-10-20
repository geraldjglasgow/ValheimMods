using System;
using System.Collections.Generic;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A zone's wild spawner (SpawnSystem.UpdateSpawning) on the server. The game's returns at once without a local player,
    /// which a dedicated server never has, and takes the game's raid spawns from the event the local player stands in. On
    /// the server it runs the same lists - the biome's, the raid's, the corner biomes' - with the raid's spawns given to a
    /// zone only while one of the players in it stands inside the raid's area (any player for an event an admin forced),
    /// so each zone spawns exactly what its own player's game would have. On a hosted game too: the host's own active event
    /// would otherwise decide the raid spawns in every zone it runs, a friend's far away included.
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawning))]
    internal static class ServerSpawning
    {
        // The game's own list: UpdateSpawnList picks the player to spawn around from it, so it must hold this zone's players.
        private static List<Player> InZone => SpawnSystem.m_tempNearPlayers;

        private static bool Prefix(SpawnSystem __instance)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            if (__instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
            {
                InZone.Clear();
                __instance.GetPlayersInZone(InZone);
                if (InZone.Count > 0)
                {
                    Spawn(__instance, ZNet.instance.GetTime());
                }
                InZone.Clear();
            }
            return false;
        }

        private static void Spawn(SpawnSystem system, DateTime time)
        {
            foreach (SpawnSystemList list in system.m_spawnLists)
            {
                system.UpdateSpawnList(list.m_spawners, time, eventSpawners: false, "b_");
            }
            List<SpawnSystem.SpawnData> raid = RaidSpawners();
            if (raid != null)
            {
                system.UpdateSpawnList(raid, time, eventSpawners: true, "e_");
            }
            for (int i = 0; i < system.m_heightmap.m_cornerAltBiomes.Count; i++)
            {
                var alt = system.m_heightmap.m_cornerAltBiomes[i];
                if (alt.m_spawn.Count > 0)
                {
                    system.UpdateSpawnList(alt.m_spawn, time, eventSpawners: false, $"m{i}");
                }
            }
        }

        // The raid's spawn list once its spawner delay has passed, for a zone with a player inside the raid's area.
        private static List<SpawnSystem.SpawnData> RaidSpawners()
        {
            RandEventSystem events = RandEventSystem.instance;
            RandomEvent raid = events.m_forcedEvent ?? events.m_randomEvent;
            if (raid == null || raid.m_time <= raid.m_spawnerDelay)
            {
                return null;
            }
            if (events.m_forcedEvent != null)
            {
                return raid.m_spawn;
            }
            foreach (Player player in InZone)
            {
                if (events.IsInsideRandomEventArea(raid, player.transform.position))
                {
                    return raid.m_spawn;
                }
            }
            return null;
        }
    }
}
