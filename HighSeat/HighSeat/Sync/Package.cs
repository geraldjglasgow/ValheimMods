using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Writes one player's "ZDOData" package in the game's own format (ZDOMan.SendZDOs): the ids the player should drop
    /// (ZDOPeer.m_invalidSector, filled by the game when an object they have moves out of their area), then the objects
    /// in order (<see cref="Records"/>) until the package is over the byte budget (checked before each, as the game does,
    /// so the last may run over), then ZDOID.None. It is written straight into the RPC's own layout, as ZRpc.Invoke would
    /// wrap it (the method's hash, then the package as a length and its bytes), and handed to the player's socket the way
    /// ZRpc.SendPackage does, its counters included: ZRpc.Invoke would first copy the whole package into a new array and
    /// then into its own, and the socket copies it again, two copies of up to 25 KB per send that the garbage collector
    /// then has to clear; here only the socket's copy is left. The buffer is reused. An object listed twice (force-sent
    /// and also near; the prepared list holds each object once, so only when something is force-sent) goes once. The
    /// game's books are kept only after the package is handed over: what the player now has (ZDOPeer.m_zdos: the
    /// revisions the record carried, <see cref="Records"/>, with Time.time as the game records; an object whose record
    /// was older than the object stays pending and force-sent until the player has its latest), the force-send and drop
    /// lists, the server's sent count (m_zdosSent); so if anything throws on the way, nothing is marked sent that was
    /// not, and the game's own loop, which then takes over, sends it. A player whose game reads deltas (HighSeatPlus,
    /// <see cref="DeltaPeers"/>) gets the same package as <see cref="DeltaFormat.DataRpc"/> instead: a version byte after
    /// the length, each object behind a kind byte, whole or as what changed since the record they hold
    /// (<see cref="DeltaWriter"/>), and an end byte; the drops, the budget and the books are the same.
    /// </summary>
    internal static class Package
    {
        private const string Method = "ZDOData";
        private static readonly int MethodHash = Method.GetStableHashCode();
        private static readonly ZPackage Pkg = new ZPackage();
        private static readonly HashSet<ZDOID> InPackage = new HashSet<ZDOID>();
        private static readonly List<ZDO> Written = new List<ZDO>();
        private static readonly List<Records.Span> Sent = new List<Records.Span>();
        private static int _lengthAt;

        /// <summary>Sends what fits of <paramref name="list"/>; nothing when it has nothing to write.</summary>
        public static void Send(ZDOMan man, PeerSync ps, List<ZDO> list, int budget)
        {
            try
            {
                bool deltas = DeltaPeers.Of(ps.Peer.m_peer) != null;
                int start = Begin(deltas);
                WriteDrops(ps.Peer.m_invalidSector);
                WriteObjects(ps.Peer, list, start + budget, deltas);
                WriteEnd(deltas);
                bool any = Written.Count > 0 || ps.Peer.m_invalidSector.Count > 0;
                if (any && Deliver(ps.Peer.m_peer.m_rpc, start))
                {
                    Settle(man, ps);
                }
            }
            finally
            {
                Written.Clear();
                Sent.Clear();
                InPackage.Clear();
            }
        }

        // The RPC's head as ZRpc.Invoke writes it (the method's hash, its name too while the game debugs RPCs), then room
        // for the payload's length, then in the delta format its version. Returns where the payload starts.
        private static int Begin(bool deltas)
        {
            Pkg.Clear();
            Pkg.Write(deltas ? DeltaFormat.DataHash : MethodHash);
            if (ZRpc.m_DEBUG)
            {
                Pkg.Write(deltas ? DeltaFormat.DataRpc : Method);
            }
            _lengthAt = Pkg.Size();
            Pkg.Write(0);
            int start = Pkg.Size();
            if (deltas)
            {
                Pkg.Write((byte)DeltaFormat.Version);
            }
            return start;
        }

        private static void WriteEnd(bool deltas)
        {
            if (deltas)
            {
                Pkg.Write(DeltaFormat.End);
            }
            else
            {
                Pkg.Write(ZDOID.None);
            }
        }

        // ZRpc.Invoke's own check and ZRpc.SendPackage's counters and send, with the payload's length filled in first.
        // False when the connection is gone, so nothing is marked sent.
        private static bool Deliver(ZRpc rpc, int start)
        {
            if (!rpc.IsConnected())
            {
                return false;
            }
            int size = Pkg.Size();
            Pkg.m_stream.Position = _lengthAt;
            Pkg.m_writer.Write(size - start);
            Pkg.m_writer.Flush();
            Pkg.m_stream.Position = size;
            rpc.m_sentPackages++;
            rpc.m_sentData += size;
            rpc.m_socket.Send(Pkg);
            return true;
        }

        private static void WriteDrops(HashSet<ZDOID> drops)
        {
            Pkg.Write(drops.Count);
            foreach (ZDOID id in drops)
            {
                Pkg.Write(id);
            }
        }

        // Only a force-sent object can be listed twice (the prepared list holds each once), so the set is needed only then.
        private static void WriteObjects(ZDOMan.ZDOPeer peer, List<ZDO> list, int limit, bool deltas)
        {
            bool twice = peer.m_forceSend.Count > 0;
            foreach (ZDO zdo in list)
            {
                if (Pkg.Size() > limit)
                {
                    break;
                }
                if (!twice || InPackage.Add(zdo.m_uid))
                {
                    Sent.Add(deltas ? DeltaWriter.Write(Pkg, zdo, peer) : Records.Write(Pkg, zdo, peer));
                    Written.Add(zdo);
                }
            }
        }

        private static void Settle(ZDOMan man, PeerSync ps)
        {
            ZDOMan.ZDOPeer peer = ps.Peer;
            peer.m_invalidSector.Clear();
            float time = Time.time;
            bool forced = peer.m_forceSend.Count > 0;
            for (int i = 0; i < Written.Count; i++)
            {
                ZDO zdo = Written[i];
                Records.Span sent = Sent[i];
                peer.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(sent.DataRevision, sent.OwnerRevision, time);
                if (sent.DataRevision == zdo.DataRevision && sent.OwnerRevision == zdo.OwnerRevision)
                {
                    if (forced)
                    {
                        peer.m_forceSend.Remove(zdo.m_uid);
                    }
                    ps.Pending.Remove(zdo.m_uid);
                }
            }
            man.m_zdosSent += Written.Count;
            SyncStats.Sent(ps, Pkg.Size(), Written.Count);
        }
    }
}
