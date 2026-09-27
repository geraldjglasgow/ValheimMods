using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Privileged edits (admin tools, the admin limit override) go to the server first. The server checks that the
    /// sending peer is on its admin list and forwards the edit to the owner of each compiler named in the request; the
    /// owner trusts the privileged flags only because the server is the RPC sender. A compiler the server does not
    /// know yet (created a moment ago by the sender) is addressed to the sender, who owns it.
    /// </summary>
    public static class ServerRelay
    {
        public const string RelayRpc = "EW_RelayEdit";

        private static ZRoutedRpc registeredOn;

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            rpc.Register<ZPackage>(RelayRpc, (sender, pkg) => Safe.Run("EarthWright relay", () => OnRelay(sender, pkg)));
        }

        /// <summary>Sender: asks the server to forward the edit to the given compilers.</summary>
        public static void Send(TerrainEdit edit, List<TerrainComp> comps)
        {
            EnsureRegistered();
            ZPackage pkg = new ZPackage();
            pkg.Write(comps.Count);
            foreach (TerrainComp comp in comps)
            {
                pkg.Write(comp.m_nview.GetZDO().m_uid);
                pkg.Write(EditWire.Write(Dispatcher.PartFor(comp, edit) ?? edit));
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(RelayRpc, pkg);
        }

        private static void OnRelay(long sender, ZPackage pkg)
        {
            if (!Side.IsServer)
                return;
            bool admin = Side.PeerIsAdmin(sender);
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                ZDOID id = pkg.ReadZDOID();
                ZPackage editPkg = pkg.ReadPackage();
                if (admin)
                    Forward(sender, id, editPkg);
            }
            if (!admin)
                Refusals.Send(sender, Refusals.NotAdmin);
        }

        private static void Forward(long sender, ZDOID id, ZPackage editPkg)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            long target = zdo != null && zdo.GetOwner() != 0L ? zdo.GetOwner() : sender;
            ZRoutedRpc.instance.InvokeRoutedRPC(target, id, OwnerHandler.RpcName, editPkg);
        }
    }
}
