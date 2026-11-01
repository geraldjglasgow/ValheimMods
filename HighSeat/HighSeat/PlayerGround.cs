using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// The ground under every ready player's area, on the server, from <see cref="ZoneKeeping"/>. A built zone is poked
    /// (its timer reset, as the game resets the zones round its own position). A missing one is asked of the game's
    /// terrain builder a few at a time, not all at once as the game's own CreateLocalZones asks for its area: the builder
    /// is one thread that keeps only its last 16 finished heightmaps, and a zone being made takes its heightmap from
    /// there or waits for it behind everything queued. Asked once per player, 50 players spread over the world queued
    /// over a thousand; finished ones were thrown away before they were taken, and a zone made then waited 30-40 s
    /// behind the queue (measured 2026-11-01). So at most MaxAsked of ours wait at a time and none are added while the
    /// builder's queue is long; every player's own zone comes first, then the ring round each, nearest first; and a pass
    /// makes at most MaxBuilds zones, the rest once BudgetMs is spent. Players in one zone share it (a crowd of 100 asked
    /// 100 times a pass, 38 ms in one frame, measured 2026-10-31).
    /// </summary>
    internal static class PlayerGround
    {
        private const int MaxAsked = 8;
        private const int MaxQueue = 8;
        private const int MaxBuilds = 4;
        private const double BudgetMs = 3;

        private static readonly double TicksPerMs = Stopwatch.Frequency / 1000.0;
        private static readonly List<Vector2s> Centres = new List<Vector2s>();
        private static readonly HashSet<Vector2s> CentreSet = new HashSet<Vector2s>();
        private static readonly HashSet<Vector2s> Wanted = new HashSet<Vector2s>();
        private static readonly HashSet<Vector2s> Asked = new HashSet<Vector2s>();
        private static readonly Predicate<Vector2s> Settled = zone => !Wanted.Contains(zone) || _zones.m_zones.ContainsKey(zone);
        private static ZoneSystem _zones;
        private static int _room;
        private static int _builds;
        private static long _start;

        public static void Keep(ZoneSystem zones)
        {
            _zones = zones;
            Gather();
            Wanted.Clear();
            _room = MaxQueue - BuilderQueue();
            _builds = 0;
            _start = Stopwatch.GetTimestamp();
            int reach = zones.m_simulationDistance.NearSimulationDistance;
            for (int ring = 0; ring <= reach; ring++)
            {
                foreach (Vector2s centre in Centres)
                {
                    Ring(centre, ring, reach);
                }
            }
            Asked.RemoveWhere(Settled);
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
            if (!Wanted.Add(zone))
            {
                return;
            }
            if (_zones.m_zones.TryGetValue(zone, out ZoneSystem.ZoneData data))
            {
                data.m_ttl = 0f;
                return;
            }
            if (MayBuild() && MayAsk(zone) && _zones.PokeLocalZone(zone))
            {
                _builds++;
            }
        }

        private static bool MayBuild()
        {
            if (_builds == 0)
            {
                return true;
            }
            return _builds < MaxBuilds && (Stopwatch.GetTimestamp() - _start) / TicksPerMs < BudgetMs;
        }

        // A zone asked before is asked again (its heightmap may be ready now); a new one only while few wait.
        private static bool MayAsk(Vector2s zone)
        {
            if (Asked.Contains(zone))
            {
                return true;
            }
            if (Asked.Count >= MaxAsked || _room <= 0)
            {
                return false;
            }
            Asked.Add(zone);
            _room--;
            return true;
        }

        // What the terrain builder still has to build, everyone's asks together.
        private static int BuilderQueue()
        {
            HeightmapBuilder builder = HeightmapBuilder.instance;
            if (builder == null)
            {
                return 0;
            }
            lock (builder.m_lock)
            {
                return builder.m_toBuild.Count;
            }
        }
    }
}
