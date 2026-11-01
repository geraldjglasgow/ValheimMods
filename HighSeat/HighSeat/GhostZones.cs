using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The game's ghost zones on the server (ZoneSystem.CreateGhostZones, ten times a second for its own position and
    /// then every player's): the land within the whole simulation distance of each that has never been generated is
    /// generated, its objects made and let go, one zone a call. The game asks the terrain builder for every such zone in
    /// each call, and 50 players spread out at the edge of explored land froze the server 4-13 s at a time while the
    /// requests piled up (measured 2026-11-01). Here, on the server while it runs creatures, the zones are walked in the
    /// game's order and made the game's way, but only as <see cref="TerrainAsks"/> allows, shared with the players'
    /// ground, so new land still comes, a few zones at a time.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateGhostZones))]
    internal static class GhostZones
    {
        private static bool Prefix(ZoneSystem __instance, Vector3 refPoint, ref bool __result)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            __result = Generate(__instance, ZoneSystem.GetZone(refPoint));
            return false;
        }

        // The zone itself, then every zone within the total distance; true once one is made, as the game stops there.
        private static bool Generate(ZoneSystem zones, Vector2s centre)
        {
            if (!zones.IsZoneGenerated(centre) && Make(zones, centre))
            {
                return true;
            }
            int total = zones.m_simulationDistance.TotalSimulationDistance;
            for (int y = centre.y - total; y <= centre.y + total; y++)
            {
                for (int x = centre.x - total; x <= centre.x + total; x++)
                {
                    var zone = new Vector2s(x, y);
                    if (zones.IsZoneGenerated(zone))
                    {
                        continue;
                    }
                    bool inside = zones.m_simulationDistance.IsClassic || zones.ZonesWithinRadius(centre, zone, total, ghostZone: true);
                    if (inside && Make(zones, zone))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // A zone never generated, made as a ghost when the shared allowance lets it.
        private static bool Make(ZoneSystem zones, Vector2s zone)
        {
            if (!TerrainAsks.MayMake(zone) || !zones.SpawnZone(zone, ZoneSystem.SpawnMode.Ghost, out _))
            {
                return false;
            }
            TerrainAsks.Made();
            return true;
        }
    }
}
