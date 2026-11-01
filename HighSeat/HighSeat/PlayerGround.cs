using System;
using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The ground under every ready player's area, on the server, from <see cref="ZoneKeeping"/>. A built zone is poked
    /// (its timer reset, as the game resets the zones round its own position). A missing one is made through the game's
    /// own PokeLocalZone, a few at a time (<see cref="TerrainAsks"/>), not all at once as the game's own
    /// CreateLocalZones asks for its area: every player's own zone first, then the ring round each, nearest first.
    /// Players in one zone share it (a crowd of 100 asked 100 times a pass, 38 ms in one frame, measured 2026-10-31).
    /// </summary>
    internal static class PlayerGround
    {
        private static readonly List<Vector2s> Centres = new List<Vector2s>();
        private static readonly HashSet<Vector2s> CentreSet = new HashSet<Vector2s>();
        private static readonly HashSet<Vector2s> Seen = new HashSet<Vector2s>();
        private static ZoneSystem _zones;

        public static void Keep(ZoneSystem zones)
        {
            _zones = zones;
            Gather();
            Seen.Clear();
            int reach = zones.m_simulationDistance.NearSimulationDistance;
            for (int ring = 0; ring <= reach; ring++)
            {
                foreach (Vector2s centre in Centres)
                {
                    Ring(centre, ring, reach);
                }
            }
        }

        // Each ready player's zone once, in the order they joined.
        private static void Gather()
        {
            Centres.Clear();
            CentreSet.Clear();
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                Vector2s zone = ZoneSystem.GetZone(peer.GetRefPos());
                if (peer.IsReady() && CentreSet.Add(zone))
                {
                    Centres.Add(zone);
                }
            }
        }

        // The zones that many steps out from the centre, inside the game's own area shape.
        private static void Ring(Vector2s centre, int ring, int reach)
        {
            for (int y = -ring; y <= ring; y++)
            {
                for (int x = -ring; x <= ring; x++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) != ring)
                    {
                        continue;
                    }
                    var zone = new Vector2s(centre.x + x, centre.y + y);
                    if (_zones.m_simulationDistance.IsClassic || _zones.ZonesWithinRadius(centre, zone, reach))
                    {
                        Keep(zone);
                    }
                }
            }
        }

        private static void Keep(Vector2s zone)
        {
            if (!Seen.Add(zone))
            {
                return;
            }
            if (_zones.m_zones.TryGetValue(zone, out ZoneSystem.ZoneData data))
            {
                data.m_ttl = 0f;
                return;
            }
            if (TerrainAsks.MayMake(zone) && _zones.PokeLocalZone(zone))
            {
                TerrainAsks.Made();
            }
        }
    }
}
