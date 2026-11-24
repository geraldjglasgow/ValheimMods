using System;
using System.Collections.Generic;
using DevBridge.Events;

namespace DevBridge.Load
{
    /// <summary>
    /// A fake player that reads HighSeat's smaller world updates (/load?players=N&amp;deltas=1), as a player's game with
    /// HighSeatPlus does: right after it joins, its hello (HighSeat's "HighSeatPlus_Deltas" with the protocol version)
    /// is handed to the server's own ZRpc for its connection as if it had come in over the network (ZRpc.HandlePackage,
    /// where every received call goes), and HighSeat's handler, registered on every peer by ZDOMan.AddPeer, takes it.
    /// From then on the server sends it "HighSeat_ZDODeltas" in place of "ZDOData" (its socket counts both as world
    /// data). Its asks to have objects sent whole again (a verifying fake's, <see cref="FakeShadow"/>) go the same way,
    /// "HighSeatPlus_FullCopy" with a count and the ids, on its tick (<see cref="Tick"/>), after the send that carried
    /// the delta is booked. The names and version are HighSeat's (HighSeat.DeltaFormat); a server without HighSeat, or
    /// with HighSeat's Big server sync off, keeps sending the game's format and the hello changes nothing.
    /// </summary>
    internal static class FakeDeltas
    {
        internal const int Version = 1;
        internal const string HelloRpc = "HighSeatPlus_Deltas";
        internal const string DataRpc = "HighSeat_ZDODeltas";
        internal const string FullCopyRpc = "HighSeatPlus_FullCopy";
        internal static readonly int DataHash = DataRpc.GetStableHashCode();
        private const int MaxAsk = 4096;

        /// <summary>Before it joins: whether the fake will say hello, and its copy of the world when it verifies.</summary>
        internal static void Prepare(FakePlayer fake, bool deltas, bool verify)
        {
            fake.Deltas = deltas;
            if (verify) fake.Socket.Shadow = new FakeShadow();
        }

        /// <summary>The fake's hello, as if it had arrived on its connection.</summary>
        internal static void Hello(FakePlayer fake)
        {
            var pkg = Call(HelloRpc);
            pkg.Write(Version);
            Deliver(fake, pkg);
        }

        /// <summary>Each frame for each fake: its waiting asks go to the server in one call.</summary>
        internal static void Tick(FakePlayer fake)
        {
            FakeShadow shadow = fake.Socket.Shadow;
            if (shadow == null || shadow.ToAsk.Count == 0) return;
            int count = Math.Min(shadow.ToAsk.Count, MaxAsk);
            var ids = new ZPackage();
            ids.Write(count);
            for (int i = 0; i < count; i++) ids.Write(shadow.ToAsk[i]);
            shadow.ToAsk.RemoveRange(0, count);
            var pkg = Call(FullCopyRpc);
            pkg.Write(ids);
            Deliver(fake, pkg);
            shadow.AsksSent += count;
        }

        // A call as ZRpc.Invoke writes it: the method's hash, its name too while the game debugs calls, the arguments next.
        private static ZPackage Call(string method)
        {
            var pkg = new ZPackage();
            pkg.Write(method.GetStableHashCode());
            if (ZRpc.m_DEBUG) pkg.Write(method);
            return pkg;
        }

        private static void Deliver(FakePlayer fake, ZPackage pkg)
        {
            try
            {
                pkg.SetPos(0);
                fake.Peer.m_rpc.HandlePackage(pkg);
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (a fake player's delta call to the server)", error);
            }
        }

        /// <summary>For the report: whether the server's handler for the hello is registered on this fake's connection.</summary>
        internal static bool ServerListens(FakePlayer fake)
        {
            Dictionary<int, ZRpc.RpcMethodBase> functions = fake.Peer.m_rpc.m_functions;
            return functions != null && functions.ContainsKey(HelloRpc.GetStableHashCode());
        }
    }
}
