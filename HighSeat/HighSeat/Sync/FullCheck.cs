using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The game's own look at a player's area, into their pending set: every object there the player lacks or has an
    /// older revision of (ZDOPeer.ShouldSend), found with the game's own sector walks. Used for each sector a player's
    /// area newly takes in (<see cref="Interest"/>: ZDOMan.FindObjects for a near sector, FindDistantObjects for the
    /// distant ring), and as the safety net: at the start of each round one player, in turn and none more often than
    /// every <see cref="NetSeconds"/> s, has their whole area checked as the game checks it on every send
    /// (FindSectorObjects), so a change no hook saw (another mod writing a ZDO's data or revision directly) still goes
    /// out within seconds. With 100 players each is checked about every 5 s, one a round; with few, every 2 s. A player
    /// still working through a backlog (<see cref="Backlog"/>, after joining or a long jump) is checked once it is done.
    /// </summary>
    internal static class FullCheck
    {
        private const float NetSeconds = 2f;

        private static readonly List<ZDO> NearObjects = new List<ZDO>();
        private static readonly List<ZDO> DistantObjects = new List<ZDO>();
        private static readonly HashSet<ZoneSystem.SectorIndex> Visited = new HashSet<ZoneSystem.SectorIndex>();
        private static int _cursor;

        /// <summary>Lets go of any ZDO references a check left behind (one that threw half way).</summary>
        public static void Clear()
        {
            NearObjects.Clear();
            DistantObjects.Clear();
            Visited.Clear();
        }

        /// <summary>One sector, all of its objects (near) or its Distant ones (the distant ring).</summary>
        public static void Sector(ZDOMan man, PeerSync ps, int x, int y, bool near)
        {
            Clear();
            Vector2s sector = new Vector2s(x, y);
            if (near)
            {
                man.FindObjects(sector, NearObjects, Visited);
            }
            else
            {
                man.FindDistantObjects(sector, NearObjects, Visited);
            }
            AddWanted(ps, NearObjects);
        }

        /// <summary>The next player in turn whose last full check is old enough gets one now.</summary>
        public static void SafetyNet(ZDOMan man, List<PeerSync> peers, float now)
        {
            for (int tried = 0; tried < peers.Count; tried++)
            {
                _cursor = (_cursor + 1) % peers.Count;
                PeerSync ps = peers[_cursor];
                if (now - ps.LastFullCheck < NetSeconds || Backlog.Left(ps) > 0)
                {
                    continue;
                }
                ps.LastFullCheck = now;
                Clear();
                man.FindSectorObjects(ps.Zone, ps.Shape.Sim, NearObjects, DistantObjects);
                AddWanted(ps, NearObjects);
                AddWanted(ps, DistantObjects);
                return;
            }
        }

        private static void AddWanted(PeerSync ps, List<ZDO> objects)
        {
            foreach (ZDO zdo in objects)
            {
                if (ps.Peer.ShouldSend(zdo))
                {
                    ps.Pending.Add(zdo.m_uid);
                }
            }
            objects.Clear();
        }
    }
}
