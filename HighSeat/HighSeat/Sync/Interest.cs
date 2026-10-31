using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// Which players' areas hold each sector, so a changed object is handed only to the players the game would send it
    /// to (<see cref="Changes"/>): a table over the game's own sector grid (ZoneSystem.SectorToIndex, the index
    /// ZDOMan.m_objectsBySector uses), each entry the players whose near area holds that sector and those whose distant
    /// ring does. A player's entries change only when their zone or their simulation distance does, by the difference:
    /// sectors left are dropped, sectors whose level changed are updated, and every sector the player now gets more of
    /// (new, or distant becoming near) is checked in full once (<see cref="FullCheck"/>), since they may lack what is in
    /// it. A first registration is a full check of the whole area, the game's first send. Sectors off the grid (beyond
    /// 16 km, past the world's edge, all filed by the game under sector 0) are left out, and so is sector 0 itself.
    /// </summary>
    internal static class Interest
    {
        /// <summary>One player watching a sector: all of it (near) or only its Distant objects (the distant ring).</summary>
        public struct Watch
        {
            public PeerSync Peer;
            public bool Near;
        }

        private static List<Watch>[] _bySector = new List<Watch>[0];
        private static readonly Stack<List<Watch>> Spare = new Stack<List<Watch>>();

        /// <summary>A new session: the table sized to the game's sector grid, empty.</summary>
        public static void Reset(ZDOMan man)
        {
            _bySector = new List<Watch>[man.m_objectsBySector.Length];
            Spare.Clear();
        }

        /// <summary>Lets go of the table (the loop stopped or the world closed): every sector then has no watchers.</summary>
        public static void Clear()
        {
            _bySector = new List<Watch>[0];
            Spare.Clear();
        }

        /// <summary>The players watching a sector (ZoneSystem.SectorIndex.Sector), or null when none.</summary>
        public static List<Watch> At(uint sector)
        {
            return sector < _bySector.Length ? _bySector[sector] : null;
        }

        /// <summary>Registers the player for their area as it stands (zone of their reference position, their setting).</summary>
        public static void Update(ZDOMan man, PeerSync ps)
        {
            ZNetPeer net = ps.Peer.m_peer;
            Vector2s zone = ZoneSystem.GetZone(net.GetRefPos());
            SimulationDistance sim = net.m_simulationDistance;
            if (ps.HasArea && zone == ps.Zone && sim.Equals(ps.Shape.Sim))
            {
                return;
            }
            AreaShape shape = AreaShape.For(sim);
            if (ps.HasArea)
            {
                Leave(ps, zone, shape);
            }
            Enter(man, ps, zone, shape);
            ps.Zone = zone;
            ps.Shape = shape;
            ps.HasArea = true;
        }

        /// <summary>Takes the player out of every sector of their area (they left).</summary>
        public static void Remove(PeerSync ps)
        {
            if (!ps.HasArea)
            {
                return;
            }
            int r = ps.Shape.Radius;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (ps.Shape.At(dx, dy) != AreaShape.Out)
                    {
                        Set(ps.Zone.x + dx, ps.Zone.y + dy, ps, AreaShape.Out);
                    }
                }
            }
            ps.HasArea = false;
        }

        // The sectors of the old area: dropped where the new area has none of them, updated where its level differs.
        private static void Leave(PeerSync ps, Vector2s zone, AreaShape shape)
        {
            int r = ps.Shape.Radius;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    byte was = ps.Shape.At(dx, dy);
                    int x = ps.Zone.x + dx;
                    int y = ps.Zone.y + dy;
                    byte now = shape.At(x - zone.x, y - zone.y);
                    if (was != AreaShape.Out && now != was)
                    {
                        Set(x, y, ps, now);
                    }
                }
            }
        }

        // The sectors of the new area: added where the old area had none of them, checked in full where the player now
        // gets more of them than before. Runs before the player's zone and shape are replaced, so those are the old ones.
        private static void Enter(ZDOMan man, PeerSync ps, Vector2s zone, AreaShape shape)
        {
            int r = shape.Radius;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    byte now = shape.At(dx, dy);
                    int x = zone.x + dx;
                    int y = zone.y + dy;
                    byte was = ps.HasArea ? ps.Shape.At(x - ps.Zone.x, y - ps.Zone.y) : AreaShape.Out;
                    if (now != AreaShape.Out && was == AreaShape.Out)
                    {
                        Set(x, y, ps, now);
                    }
                    if (now > was && OnGrid(x, y))
                    {
                        FullCheck.Sector(man, ps, x, y, now == AreaShape.Near);
                    }
                }
            }
        }

        private static bool OnGrid(int x, int y)
        {
            uint sector = ZoneSystem.SectorToIndex(x, y).Sector;
            return sector != 0 && sector < _bySector.Length;
        }

        // Sets the player's level for one sector: out removes the entry, near or distant adds or replaces it.
        private static void Set(int x, int y, PeerSync ps, byte level)
        {
            uint sector = ZoneSystem.SectorToIndex(x, y).Sector;
            if (sector == 0 || sector >= _bySector.Length)
            {
                return;
            }
            List<Watch> list = _bySector[sector];
            int at = list == null ? -1 : IndexOf(list, ps);
            if (level == AreaShape.Out)
            {
                Unwatch(sector, list, at);
                return;
            }
            Watch watch = new Watch { Peer = ps, Near = level == AreaShape.Near };
            if (at >= 0)
            {
                list[at] = watch;
                return;
            }
            AddWatch(sector, watch);
        }

        private static void AddWatch(uint sector, Watch watch)
        {
            List<Watch> list = _bySector[sector];
            if (list == null)
            {
                list = Spare.Count > 0 ? Spare.Pop() : new List<Watch>();
                _bySector[sector] = list;
            }
            list.Add(watch);
        }

        // Removes one entry by moving the last one into its place; an emptied list is kept for reuse.
        private static void Unwatch(uint sector, List<Watch> list, int at)
        {
            if (at < 0)
            {
                return;
            }
            int last = list.Count - 1;
            list[at] = list[last];
            list.RemoveAt(last);
            if (list.Count == 0)
            {
                _bySector[sector] = null;
                Spare.Push(list);
            }
        }

        private static int IndexOf(List<Watch> list, PeerSync ps)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Peer == ps)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
