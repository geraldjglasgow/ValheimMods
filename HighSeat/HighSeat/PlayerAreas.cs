using System.Collections.Generic;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The server loads every player's area, not only its own (ZNetScene.CreateDestroyObjects, 30 times a second): the
    /// near sectors around its own position and around every ready player, each sector once, then the game's own create
    /// and remove. Distant objects (seen from afar, never simulated) are kept for a hosting player's own view only; a
    /// dedicated server draws nothing. The lists are the game's own (m_tempCurrentObjects, m_tempCurrentDistantObjects),
    /// filled as the game fills them and left filled after, because AreaLoading (Wayfare's quick jumps, OpenKeep's bed
    /// respawn) reads them in a postfix on the same method to load a hosting player's arrival faster.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateDestroyObjects))]
    internal static class PlayerAreas
    {
        private static List<ZDO> Near = null!;
        private static List<ZDO> Distant = null!;
        private static readonly HashSet<ZoneSystem.SectorIndex> Visited = new HashSet<ZoneSystem.SectorIndex>();

        private static bool Prefix(ZNetScene __instance)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            Near = __instance.m_tempCurrentObjects;
            Distant = __instance.m_tempCurrentDistantObjects;
            Near.Clear();
            Distant.Clear();
            Collect();
            __instance.CreateObjects(Near, Distant);
            __instance.RemoveObjects(Near, Distant);
            return false;
        }

        private static void Collect()
        {
            Visited.Clear();
            SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
            Vector2s own = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
            AddNear(own, sim);
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    AddNear(ZoneSystem.GetZone(peer.GetRefPos()), sim);
                }
            }
            if (!ZNet.instance.IsDedicated())
            {
                AddDistant(own, sim);
            }
        }

        private static void AddNear(Vector2s center, SimulationDistance sim)
        {
            int n = sim.NearSimulationDistance;
            for (int dy = -n; dy <= n; dy++)
            {
                for (int dx = -n; dx <= n; dx++)
                {
                    Vector2s sector = new Vector2s(center.x + dx, center.y + dy);
                    if ((dx == 0 && dy == 0) || sim.IsClassic || ZoneSystem.instance.ZonesWithinRadius(center, sector, n))
                    {
                        ZDOMan.instance.FindObjects(sector, Near, Visited);
                    }
                }
            }
        }

        // The game's distant ring around the host's own position; sectors already found near are skipped by Visited.
        private static void AddDistant(Vector2s center, SimulationDistance sim)
        {
            int total = sim.TotalSimulationDistance;
            for (int dy = -total; dy <= total; dy++)
            {
                for (int dx = -total; dx <= total; dx++)
                {
                    Vector2s sector = new Vector2s(center.x + dx, center.y + dy);
                    if (sim.IsClassic || ZoneSystem.instance.ZonesWithinRadius(center, sector, total, ghostZone: true))
                    {
                        ZDOMan.instance.FindDistantObjects(sector, Distant, Visited);
                    }
                }
            }
        }
    }
}
