using System.Collections.Generic;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// The game's own look at a player's area, into their pending set: every object there the player lacks or has an
    /// older revision of (ZDOPeer.ShouldSend), found with the game's own sector walks (ZDOMan.FindObjects for a near
    /// sector, FindDistantObjects for the distant ring). Used for each sector a player's area newly takes in
    /// (<see cref="Interest"/>; a new player's whole area), and as the safety net: at the start of each round one player,
    /// in turn and none more often than every <see cref="NetSeconds"/> s, has their whole area looked at as the game looks
    /// at it on every send (sector by sector, as ZDOMan.FindSectorObjects walks it), so a change no hook saw (another mod
    /// writing a ZDO's data or revision directly) still goes out within seconds. (Every 2 s cost a whole area scan in
    /// every round with 50 players, 0.7 ms each, measured 2026-10-31.) A player still working through a backlog
    /// (<see cref="Backlog"/>, after joining or a long jump) is looked at once it is done. Every look is queued, one sector
    /// at a time, and run within part of the send loop's frame time (<see cref="Run"/>): a crowd crossing into new zones
    /// at a dense base checked those zones at once and held up the round's first frame, 1.5 ms on average with 100
    /// players; now the looks wait for the next frames' time. A player who left meanwhile has their looks dropped.
    /// </summary>
    internal static class FullCheck
    {
        private const float NetSeconds = 8f;

        private struct Look
        {
            public PeerSync Peer;
            public int X;
            public int Y;
            public bool Near;
        }

        private static readonly Queue<Look> Queued = new Queue<Look>();
        private static readonly List<ZDO> Objects = new List<ZDO>();
        private static readonly HashSet<ZoneSystem.SectorIndex> Visited = new HashSet<ZoneSystem.SectorIndex>();
        private static int _cursor;

        /// <summary>Forgets every queued look and lets go of any ZDO references a look left behind (one that threw).</summary>
        public static void Clear()
        {
            Queued.Clear();
            Objects.Clear();
            Visited.Clear();
        }

        /// <summary>Queues one sector: all of its objects (near) or its Distant ones (the distant ring).</summary>
        public static void Sector(PeerSync ps, int x, int y, bool near)
        {
            Queued.Enqueue(new Look { Peer = ps, X = x, Y = y, Near = near });
        }

        /// <summary>Runs queued looks while <paramref name="clock"/> is under <paramref name="until"/> ticks.</summary>
        public static void Run(ZDOMan man, Stopwatch clock, long until)
        {
            while (Queued.Count > 0 && clock.ElapsedTicks < until)
            {
                Look look = Queued.Dequeue();
                if (!look.Peer.Gone)
                {
                    Do(man, look);
                }
            }
        }

        private static void Do(ZDOMan man, Look look)
        {
            Objects.Clear();
            Visited.Clear();
            Vector2s sector = new Vector2s(look.X, look.Y);
            if (look.Near)
            {
                man.FindObjects(sector, Objects, Visited);
            }
            else
            {
                man.FindDistantObjects(sector, Objects, Visited);
            }
            foreach (ZDO zdo in Objects)
            {
                if (look.Peer.Peer.ShouldSend(zdo))
                {
                    look.Peer.Pending.Add(zdo.m_uid);
                }
            }
            Objects.Clear();
        }

        /// <summary>The next player in turn whose last full look is old enough has their whole area queued now.</summary>
        public static void SafetyNet(List<PeerSync> peers, float now)
        {
            for (int tried = 0; tried < peers.Count; tried++)
            {
                _cursor = (_cursor + 1) % peers.Count;
                PeerSync ps = peers[_cursor];
                if (!ps.HasArea || now - ps.LastFullCheck < NetSeconds || Backlog.Left(ps) > 0)
                {
                    continue;
                }
                ps.LastFullCheck = now;
                QueueArea(ps);
                return;
            }
        }

        // Every sector of the area at its level, as ZDOMan.FindSectorObjects would walk it (AreaShape matches it).
        private static void QueueArea(PeerSync ps)
        {
            int r = ps.Shape.Radius;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    byte level = ps.Shape.At(dx, dy);
                    if (level != AreaShape.Out)
                    {
                        Sector(ps, ps.Zone.x + dx, ps.Zone.y + dy, level == AreaShape.Near);
                    }
                }
            }
        }
    }
}
