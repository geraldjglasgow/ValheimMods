using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Writes one player's "ZDOData" package in the game's own format (ZDOMan.SendZDOs): the ids the player should drop
    /// (ZDOPeer.m_invalidSector, filled by the game when an object they have moves out of their area), then the objects
    /// in order (<see cref="Records"/>) until the package is over the byte budget (checked before each, as the game
    /// does, so the last may run over), then ZDOID.None. Sent with the game's own RPC, which copies the package at once
    /// (ZRpc.Invoke serializes it into its own), so it is reused. An object listed twice (force-sent and also near) goes
    /// once. The game's books are kept only after the package is handed over: what the player now has
    /// (ZDOPeer.m_zdos, with Time.time as the game records), the force-send and drop lists, the server's sent count
    /// (m_zdosSent); so if anything throws on the way, nothing is marked sent that was not, and the game's own loop,
    /// which then takes over, sends it.
    /// </summary>
    internal static class Package
    {
        private static readonly ZPackage Pkg = new ZPackage();
        private static readonly object[] Args = new object[1];
        private static readonly HashSet<ZDOID> InPackage = new HashSet<ZDOID>();
        private static readonly List<ZDO> Written = new List<ZDO>();

        /// <summary>Sends what fits of <paramref name="list"/>; nothing when it has nothing to write.</summary>
        public static void Send(ZDOMan man, PeerSync ps, List<ZDO> list, int budget)
        {
            try
            {
                Pkg.Clear();
                WriteDrops(ps.Peer.m_invalidSector);
                WriteObjects(list, budget);
                Pkg.Write(ZDOID.None);
                if (Written.Count > 0 || ps.Peer.m_invalidSector.Count > 0)
                {
                    Args[0] = Pkg;
                    ps.Peer.m_peer.m_rpc.Invoke("ZDOData", Args);
                    Settle(man, ps);
                }
            }
            finally
            {
                Args[0] = null;
                Written.Clear();
                InPackage.Clear();
            }
        }

        private static void WriteDrops(HashSet<ZDOID> drops)
        {
            Pkg.Write(drops.Count);
            foreach (ZDOID id in drops)
            {
                Pkg.Write(id);
            }
        }

        private static void WriteObjects(List<ZDO> list, int budget)
        {
            foreach (ZDO zdo in list)
            {
                if (Pkg.Size() > budget)
                {
                    break;
                }
                if (InPackage.Add(zdo.m_uid))
                {
                    Records.Write(Pkg, zdo);
                    Written.Add(zdo);
                }
            }
        }

        private static void Settle(ZDOMan man, PeerSync ps)
        {
            ZDOMan.ZDOPeer peer = ps.Peer;
            peer.m_invalidSector.Clear();
            float time = Time.time;
            foreach (ZDO zdo in Written)
            {
                peer.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.DataRevision, zdo.OwnerRevision, time);
                peer.m_forceSend.Remove(zdo.m_uid);
                ps.Pending.Remove(zdo.m_uid);
            }
            man.m_zdosSent += Written.Count;
            SyncStats.Sent(ps, Pkg.Size(), Written.Count);
        }
    }
}
