using System.Collections.Generic;
using UnityEngine;

namespace Party
{
    /// <summary>One online player: persistent ID, name, peer ID, position.</summary>
    public struct OnlinePlayer
    {
        public long Id;
        public string Name;
        public long PeerId;
        public Vector3 Position;
    }

    /// <summary>Resolves persistent player identity from peers, host included.</summary>
    public static class Identity
    {
        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>Falls back to the profile so the ID survives the local player being dead (no Player object).</summary>
        public static long LocalPlayerId => Player.m_localPlayer != null
            ? Player.m_localPlayer.GetPlayerID()
            : Game.instance != null && Game.instance.GetPlayerProfile() != null ? Game.instance.GetPlayerProfile().GetPlayerID() : 0L;

        public static string LocalPlayerName => Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "";

        /// <summary>Every player the server currently has a connection to, host included. Server side only.</summary>
        public static IEnumerable<OnlinePlayer> Online()
        {
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.m_playerID != 0)
                    yield return FromPeer(peer);
            }
            if (TryHost(out OnlinePlayer host))
                yield return host;
        }

        /// <summary>Finds an online player by persistent ID. Server side only.</summary>
        public static bool TryFind(long id, out OnlinePlayer player) =>
            TryFindWhere(id, static (candidate, key) => candidate.Id == key, out player);

        /// <summary>Finds an online player by their routing peer ID (the sender of an incoming RPC). Server side only.</summary>
        public static bool TryFindByPeerId(long peerId, out OnlinePlayer player) =>
            TryFindWhere(peerId, static (candidate, key) => candidate.PeerId == key, out player);

        /// <summary>Finds an online player by name (case insensitive). Server side only.</summary>
        public static bool TryFindByName(string name, out OnlinePlayer player) =>
            TryFindWhere(name, static (candidate, key) => string.Equals(candidate.Name, key, System.StringComparison.OrdinalIgnoreCase), out player);

        /// <summary>
        /// The lookups behind the finders: a plain loop over the peers, then the host, with a static match, so the
        /// vitals relay (several reports a second per member) allocates nothing to resolve its sender and recipients.
        /// </summary>
        private static bool TryFindWhere<TKey>(TKey key, System.Func<OnlinePlayer, TKey, bool> match, out OnlinePlayer player)
        {
            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            for (int i = 0; i < peers.Count; i++)
            {
                if (peers[i].m_playerID == 0)
                    continue;
                player = FromPeer(peers[i]);
                if (match(player, key))
                    return true;
            }
            return TryHost(out player) && match(player, key);
        }

        private static OnlinePlayer FromPeer(ZNetPeer peer) =>
            new OnlinePlayer { Id = peer.m_playerID, Name = peer.m_playerName, PeerId = peer.m_uid, Position = peer.m_refPos };

        /// <summary>The host's own player on a listen server; a dedicated server has none.</summary>
        private static bool TryHost(out OnlinePlayer player)
        {
            Player local = Player.m_localPlayer;
            bool host = local != null && !ZNet.instance.IsDedicated();
            player = host
                ? new OnlinePlayer { Id = local.GetPlayerID(), Name = local.GetPlayerName(), PeerId = ZNet.GetUID(), Position = local.transform.position }
                : default;
            return host;
        }

        /// <summary>The peer routing ID to send to a given player, or the local pseudo-ID when it is the host itself.</summary>
        public static long PeerIdFor(long playerId) => TryFind(playerId, out OnlinePlayer p) ? p.PeerId : 0;
    }
}
