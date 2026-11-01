using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Who is near what, for "Creatures only where shared": the players of one ownership pass (every ready player's
    /// reference position, and on a hosted game the host's own, whose id is the server's), each listed once under every
    /// zone its active area can reach, so a creature asks only the few players of its own zone. Built once per pass (at
    /// the start of each <see cref="OwnershipRounds"/> round), never per creature per peer. "Near" is the
    /// game's own test, ZNetScene.InActiveArea around the player's zone: at most 96 m from the zone's centre on either
    /// axis (64 m at the smallest simulation distance), so the zones within <see cref="Reach"/> of a player's own are the
    /// ones it can reach. Also asked between passes with the players' live positions (<see cref="CountLive"/>), for a
    /// creature the server has just made.
    /// </summary>
    internal static class NearPlayers
    {
        private static readonly List<long> Uids = new List<long>();
        private static readonly List<Vector2s> Zones = new List<Vector2s>();
        private static readonly Dictionary<Vector2s, List<int>> ByZone = new Dictionary<Vector2s, List<int>>();
        private static readonly Stack<List<int>> Spare = new Stack<List<int>>();

        /// <summary>The players of this pass.</summary>
        public static int Count => Uids.Count;

        public static long UidOf(int index) => Uids[index];

        public static Vector2s ZoneOf(int index) => Zones[index];

        /// <summary>How many zones out from a player's own its active area reaches: 2, or 1 at the smallest distance.</summary>
        public static int Reach(SimulationDistance sim) => sim.NearSimulationDistance <= 1 ? 1 : 2;

        /// <summary>Lists this pass's players.</summary>
        public static void Build()
        {
            Clear();
            int reach = Reach(ZNet.instance.GetSyncedSimulationDistance());
            if (!ZNet.instance.IsDedicated())
            {
                Add(ZDOMan.GetSessionID(), ZNet.instance.GetReferencePosition(), reach);
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    Add(peer.m_uid, peer.GetRefPos(), reach);
                }
            }
        }

        /// <summary>The players of this pass with <paramref name="point"/> in their active area; the one when exactly one.</summary>
        public static int CountAt(Vector3 point, out long single)
        {
            single = 0L;
            if (!ByZone.TryGetValue(ZoneSystem.GetZone(point), out List<int> near))
            {
                return 0;
            }
            int count = 0;
            foreach (int index in near)
            {
                if (ZNetScene.InActiveArea(point, Zones[index]))
                {
                    count++;
                    single = Uids[index];
                }
            }
            return count;
        }

        /// <summary>The same count from where the players are this frame: every player is tried, so keep it rare.</summary>
        public static int CountLive(Vector3 point, out long single)
        {
            single = 0L;
            int count = 0;
            if (!ZNet.instance.IsDedicated() && ZNetScene.InActiveArea(point, ZNet.instance.GetReferencePosition()))
            {
                count++;
                single = ZDOMan.GetSessionID();
            }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady() && ZNetScene.InActiveArea(point, peer.GetRefPos()))
                {
                    count++;
                    single = peer.m_uid;
                }
            }
            return count;
        }

        private static void Add(long uid, Vector3 position, int reach)
        {
            int index = Uids.Count;
            Vector2s zone = ZoneSystem.GetZone(position);
            Uids.Add(uid);
            Zones.Add(zone);
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    Vector2s key = new Vector2s(zone.x + dx, zone.y + dy);
                    if (!ByZone.TryGetValue(key, out List<int> list))
                    {
                        list = Spare.Count > 0 ? Spare.Pop() : new List<int>();
                        ByZone[key] = list;
                    }
                    list.Add(index);
                }
            }
        }

        private static void Clear()
        {
            foreach (List<int> list in ByZone.Values)
            {
                list.Clear();
                Spare.Push(list);
            }
            ByZone.Clear();
            Uids.Clear();
            Zones.Clear();
        }
    }
}
