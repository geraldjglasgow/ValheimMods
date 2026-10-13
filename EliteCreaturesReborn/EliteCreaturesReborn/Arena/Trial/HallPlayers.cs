using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The living players in the hall, as this machine holds them (the game's own player list, never a scene search). The
    /// core's owner always holds them: the hall lies in the zone at the world's centre, which every machine running the
    /// core has loaded, players and all.
    /// </summary>
    internal static class HallPlayers
    {
        private static readonly List<long> Peers = new List<long>();

        /// <summary>The peers of the living players in the hall, in a list reused by the next call.</summary>
        public static List<long> PeersNow()
        {
            Peers.Clear();
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                ZDO? zdo = InHall(players[i]);
                if (zdo != null && !Peers.Contains(zdo.GetOwner()))
                {
                    Peers.Add(zdo.GetOwner());
                }
            }
            return Peers;
        }

        /// <summary>The living player in the hall nearest to <paramref name="at"/>; null when there is none.</summary>
        public static Player? Nearest(Vector3 at)
        {
            Player? best = null;
            float bestSqr = float.MaxValue;
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                float sqr = InHall(p) != null ? (p.transform.position - at).sqrMagnitude : float.MaxValue;
                if (sqr < bestSqr)
                {
                    best = p;
                    bestSqr = sqr;
                }
            }
            return best;
        }

        /// <summary>True when the local player is alive in the hall.</summary>
        public static bool LocalInHall() => InHall(Player.m_localPlayer) != null;

        // The player's ZDO when they are alive, in the game (not a ghost or a free-flying admin) and inside the hall.
        private static ZDO? InHall(Player? p)
        {
            if (p == null || p.IsDead() || p.InGhostMode() || p.InDebugFlyMode() || p.m_nview == null)
            {
                return null;
            }
            ZDO? zdo = p.m_nview.GetZDO();
            return zdo != null && ArenaHall.Contains(p.transform.position) ? zdo : null;
        }
    }
}
