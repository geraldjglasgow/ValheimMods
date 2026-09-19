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
                    yield return new OnlinePlayer { Id = peer.m_playerID, Name = peer.m_playerName, PeerId = peer.m_uid, Position = peer.m_refPos };
            }
            Player local = Player.m_localPlayer;
            if (local != null && !ZNet.instance.IsDedicated())
                yield return new OnlinePlayer { Id = local.GetPlayerID(), Name = local.GetPlayerName(), PeerId = ZNet.GetUID(), Position = local.transform.position };
        }

        /// <summary>Finds an online player by persistent ID. Server side only.</summary>
        public static bool TryFind(long id, out OnlinePlayer player)
        {
            foreach (OnlinePlayer candidate in Online())
            {
                if (candidate.Id == id)
                {
                    player = candidate;
                    return true;
                }
            }
            player = default;
            return false;
        }

        /// <summary>Finds an online player by their routing peer ID (the sender of an incoming RPC). Server side only.</summary>
        public static bool TryFindByPeerId(long peerId, out OnlinePlayer player)
        {
            foreach (OnlinePlayer candidate in Online())
            {
                if (candidate.PeerId == peerId)
                {
                    player = candidate;
                    return true;
                }
            }
            player = default;
            return false;
        }

        /// <summary>Finds an online player by name (case insensitive). Server side only.</summary>
        public static bool TryFindByName(string name, out OnlinePlayer player)
        {
            foreach (OnlinePlayer candidate in Online())
            {
                if (string.Equals(candidate.Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    player = candidate;
                    return true;
                }
            }
            player = default;
            return false;
        }

        /// <summary>The peer routing ID to send to a given player, or the local pseudo-ID when it is the host itself.</summary>
        public static long PeerIdFor(long playerId) => TryFind(playerId, out OnlinePlayer p) ? p.PeerId : 0;
    }
}
