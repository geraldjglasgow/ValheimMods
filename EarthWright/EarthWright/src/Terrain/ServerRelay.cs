using EarthWright.Core;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Privileged edits (admin tools, the admin limit override) go to the server first. The server checks that the
    /// sending peer is on its admin list and forwards the edit to the owner of each compiler named in the request; the
    /// owner trusts the privileged flags only because the server is the RPC sender. A compiler the server does not
    /// know yet, or knows without an owner (created or claimed a moment ago by the sender), is addressed to the sender,
    /// who owns it. A receiver that does not own it after all answers the sender, who sends the part again.
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

        /// <summary>Sender: asks the server to forward the compiler's part of the edit to the compiler's owner.</summary>
        public static void Send(TerrainComp comp, TerrainEdit part)
        {
            EnsureRegistered();
            ZPackage pkg = new ZPackage();
            pkg.Write(1);
            pkg.Write(comp.m_nview.GetZDO().m_uid);
            pkg.Write(EditWire.Write(part));
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
                else
                    Refuse(sender, editPkg);
            }
        }

        /// <summary>A non-admin's privileged part is answered as refused (the reason is shown to the sender).</summary>
        private static void Refuse(long sender, ZPackage editPkg)
        {
            TerrainEdit edit = EditWire.Read(editPkg, sender);
            if (edit == null || edit.RequestId == 0)
            {
                Refusals.Send(sender, Refusals.NotAdmin);
                return;
            }
            edit.SenderPeer = sender;
            EditAnswers.Send(edit, EditAnswer.Refused, Refusals.NotAdmin);
        }

        private static void Forward(long sender, ZDOID id, ZPackage editPkg)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            long target = zdo != null && zdo.GetOwner() != 0L ? zdo.GetOwner() : sender;
            ZRoutedRpc.instance.InvokeRoutedRPC(target, id, OwnerHandler.RpcName, editPkg);
        }
    }
}
