using System;
using DevBridge.Events;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// A fake player joins and leaves the way a real one does, as far as the server's own state goes. Joining: the peer
    /// goes on ZNet's peer list (what OnNewConnection does first), then everything RPC_PeerInfo does for a client that
    /// passed its checks: the simulation distance through RPC_RequestValidSimulationDistance (the fake asks for the
    /// server's own, so it gets the most the server allows), uid, name and reference position, the server's own peer info
    /// sent back, then ZDOMan.AddPeer and ZRoutedRpc.AddPeer (which sends it the global keys and location icons, as on any
    /// join); then what a client sends next: its reference position, shown on the map, and its display name
    /// (RPC_ServerSyncedPlayerData, every two seconds from <see cref="FakeTicker"/>) and its body (RPC_CharacterID, from
    /// <see cref="FakeCharacter"/>). The checks themselves (version, password, ban and permitted lists, session ticket,
    /// the player cap) and every mod's hook on OnNewConnection and RPC_PeerInfo are skipped: a fake has nothing to check,
    /// so the fakes join past the cap, and each counts towards the cap a real player meets. Its peer player id stays 0:
    /// the game never reads it, and a mod that keeps a saved roster of the peers that have one (Lockstep) would keep the
    /// fakes in it. Leaving is the game's own ZNet.Disconnect: routed RPC and ZDOMan peers removed (which destroys the
    /// body with every gone peer's non-persistent ZDOs, after a pass over every ZDO, as for any disconnect), peer list,
    /// socket disposed, player list sent.
    /// </summary>
    internal static class FakeJoin
    {
        internal static void Join(FakePlayer fake, Vector3 refPos)
        {
            ZNet net = ZNet.instance;
            ZNetPeer peer = fake.Peer;
            net.GetPeers().Add(peer);
            ValidateDistance(net, peer);
            peer.m_refPos = refPos;
            peer.m_uid = fake.Uid;
            peer.m_playerName = fake.Name;
            peer.m_playfabId = "";
            peer.m_publicRefPos = true;
            peer.m_serverSyncedPlayerData["platformDisplayName"] = fake.Name;
            SendInfo(net, peer);
            peer.m_socket.VersionMatch();
            ZDOMan.instance.AddPeer(peer);
            ZRoutedRpc.instance.AddPeer(peer);
            fake.Holdings = ZDOMan.instance.FindPeer(peer);
        }

        // As RPC_PeerInfo passes on the distance a client asks for; the fake asks for the server's own. A peer left
        // without one would be sent its own sector only, so a server that has none yet gives the synced or original one.
        private static void ValidateDistance(ZNet net, ZNetPeer peer)
        {
            var pkg = new ZPackage();
            net.m_simulationDistance.Serialize(ref pkg);
            pkg.SetPos(0);
            net.RPC_RequestValidSimulationDistance(peer.m_rpc, pkg);
            if (peer.m_simulationDistance.NearSimulationDistance > 0) return;
            SimulationDistance synced = net.GetSyncedSimulationDistance();
            peer.m_simulationDistance = synced.NearSimulationDistance > 0 ? synced : SimulationDistance.OriginalDistance;
        }

        // The server's own peer info, as RPC_PeerInfo sends every client that joins: only bytes, to a fake.
        private static void SendInfo(ZNet net, ZNetPeer peer)
        {
            try
            {
                net.SendPeerInfo(peer.m_rpc);
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (the server's peer info for a fake player)", error);
            }
        }

        /// <summary>After a batch has joined: the player, historical and admin lists to everyone, as every join sends them.</summary>
        internal static void Announce()
        {
            try
            {
                ZNet net = ZNet.instance;
                net.SendPlayerList();
                net.SendHistoricalPlayerList();
                net.SendAdminList();
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (sending the player list after fake players joined)", error);
            }
        }

        /// <summary>The game's own disconnect, while the peer is still on its list.</summary>
        internal static void Leave(FakePlayer fake)
        {
            ZNet net = ZNet.instance;
            if (net != null && net.GetPeers().Contains(fake.Peer)) net.Disconnect(fake.Peer);
        }
    }
}
