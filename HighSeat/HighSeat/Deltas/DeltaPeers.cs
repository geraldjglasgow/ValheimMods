using System;
using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The players whose game reads deltas, and the two calls they send (<see cref="DeltaFormat"/>), heard on every
    /// connection's ZRpc from the moment it opens (<see cref="DeltaHooks"/>). Hello with this version: from the next
    /// send on, that player gets <see cref="DeltaFormat.DataRpc"/>. Any other version, or none at all (a game without
    /// HighSeatPlus never says it), leaves them on the game's own "ZDOData". Hello with 0 is a player's game giving up
    /// (it could not read a package): back to the game's format for the rest of the connection (later hellos are not
    /// heard), and everything they were booked as holding is forgotten and their area checked again in full
    /// (<see cref="Interest.Remove"/>: the next round registers them as new), so every object is sent to them again. A
    /// full-copy request names objects whose delta did not fit their copy: each one they are booked as holding is
    /// forgotten for them (ZDOPeer.m_zdos) and force-sent (ZDOPeer.ForceSendZDO), so it goes whole at the front of their
    /// next package; others are left (never booked, or gone: they get it whole anyway, or not at all). Kept by the
    /// connection's ZRpc, dropped when the game removes the peer (ZDOMan.RemovePeer, which every disconnect goes
    /// through) and when the world closes. Every handler catches its own failures: a broken call is logged once and
    /// dropped, never thrown into the game's networking (ZRpc takes an EndOfStreamException for a wrong version and
    /// drops the connection).
    /// </summary>
    internal static class DeltaPeers
    {
        private static readonly Dictionary<ZRpc, DeltaPeer> ByRpc = new Dictionary<ZRpc, DeltaPeer>();
        private static readonly HashSet<ZRpc> GaveUp = new HashSet<ZRpc>();
        private static bool _warned;

        public static int Count => ByRpc.Count;

        /// <summary>True while at least one player reads deltas: only then are records kept for them.</summary>
        public static bool Any => ByRpc.Count > 0;

        /// <summary>The player's delta state, or null when they get the game's format.</summary>
        public static DeltaPeer Of(ZNetPeer peer)
        {
            return ByRpc.Count > 0 && peer.m_rpc != null && ByRpc.TryGetValue(peer.m_rpc, out DeltaPeer state) ? state : null;
        }

        /// <summary>Hears the two calls on a connection (registering again replaces the same handlers).</summary>
        public static void Listen(ZNetPeer peer)
        {
            if (peer?.m_rpc == null)
            {
                return;
            }
            peer.m_rpc.Register<int>(DeltaFormat.HelloRpc, Hello);
            peer.m_rpc.Register<ZPackage>(DeltaFormat.FullCopyRpc, FullCopy);
        }

        public static void Left(ZNetPeer peer)
        {
            if (peer?.m_rpc != null)
            {
                ByRpc.Remove(peer.m_rpc);
                GaveUp.Remove(peer.m_rpc);
            }
        }

        public static void Clear()
        {
            ByRpc.Clear();
            GaveUp.Clear();
        }

        // A connection that gave up once stays on the game's format: a game sending hello again and again cannot make the
        // server send its whole area again and again.
        private static void Hello(ZRpc rpc, int version)
        {
            try
            {
                if (GaveUp.Contains(rpc))
                {
                    return;
                }
                if (version == DeltaFormat.Version)
                {
                    ByRpc[rpc] = new DeltaPeer(version);
                }
                else if (version == 0 && ByRpc.Remove(rpc) && GaveUp.Add(rpc))
                {
                    Resend(rpc);
                }
            }
            catch (Exception e)
            {
                Warn("a player's delta hello", e);
            }
        }

        // A player's game gave up on deltas: everything is forgotten for them and their area checked again in full.
        private static void Resend(ZRpc rpc)
        {
            HighSeat.Log.LogWarning("a player's game could not read a delta package; it gets the game's own format from now on");
            ZDOMan.ZDOPeer peer = ZDOMan.instance?.FindPeer(rpc);
            if (peer == null)
            {
                return;
            }
            peer.m_zdos.Clear();
            foreach (PeerSync ps in SyncPeers.All)
            {
                if (ps.Peer == peer)
                {
                    Interest.Remove(ps);
                }
            }
        }

        private static void FullCopy(ZRpc rpc, ZPackage ids)
        {
            try
            {
                ZDOMan.ZDOPeer peer = ZNet.instance != null && ZNet.instance.IsServer() ? ZDOMan.instance?.FindPeer(rpc) : null;
                if (peer == null)
                {
                    return;
                }
                int count = Math.Min(ids.ReadInt(), DeltaFormat.MaxAskedIds);
                for (int i = 0; i < count; i++)
                {
                    ZDOID id = ids.ReadZDOID();
                    if (peer.m_zdos.Remove(id))
                    {
                        peer.ForceSendZDO(id);
                    }
                }
                #if DEBUG
                DeltaStats.Asked(Math.Max(count, 0));
                #endif
            }
            catch (Exception e)
            {
                Warn("a player's full-copy request", e);
            }
        }

        private static void Warn(string what, Exception e)
        {
            if (!_warned)
            {
                _warned = true;
                HighSeat.Log.LogWarning($"{what} could not be read and was dropped (logged once): {e.Message}");
            }
        }
    }

    /// <summary>One player who reads deltas: the protocol version they said hello with.</summary>
    internal sealed class DeltaPeer
    {
        public readonly int Version;

        public DeltaPeer(int version)
        {
            Version = version;
        }
    }
}
