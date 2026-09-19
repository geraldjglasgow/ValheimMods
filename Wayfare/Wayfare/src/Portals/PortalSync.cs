using System.Collections.Generic;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>Server-to-client portal snapshots. The game only ever sends portal ZDOs for sectors near each
    /// peer (<c>ZDOMan.CreateSyncList</c> → <c>FindSectorObjects</c>), so a pure client's own
    /// <c>ZDOMan.GetPortalList()</c> holds just the portals it has happened to be near this session - nearly
    /// empty right after login. The server is the one machine whose list is complete (the portal chunk is always
    /// loaded there), so clients ask it for a snapshot on the registry's own 5-second cadence instead of reading
    /// their local, near-only list.</summary>
    public static class PortalSync
    {
        public const string RequestRpc = "wf_RequestPortals";
        public const string ListRpc = "wf_PortalList";

        // The instance registered on, not a bool: every new session constructs a fresh ZRoutedRpc with empty
        // handler tables, and an unknown routed method is dropped silently.
        private static ZRoutedRpc registeredOn;

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            rpc.Register(RequestRpc, OnRequestPortals);
            rpc.Register<ZPackage>(ListRpc, OnPortalList);
        }

        /// <summary>Client side: ask the server for a fresh snapshot. No explicit target routes to the server.</summary>
        public static void RequestFromServer()
        {
            EnsureRegistered();
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc);
        }

        private static void OnRequestPortals(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !WayfareConfig.Enabled.Value)
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ListRpc, BuildSnapshot());
        }

        private static ZPackage BuildSnapshot()
        {
            PortalDiscovery.EnsureDiscovered();
            List<PortalInfo> portals = PortalRegistry.ReadLocal();
            ZPackage pkg = new ZPackage();
            pkg.Write(portals.Count);
            foreach (PortalInfo portal in portals)
            {
                pkg.Write(portal.Id);
                pkg.Write(portal.Position);
                pkg.Write(portal.Tag);
                pkg.Write((int)portal.Mode);
                pkg.Write(portal.Owner);
            }
            return pkg;
        }

        private static void OnPortalList(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || !SenderIdentity.IsFromServer(sender))
                return;
            int count = pkg.ReadInt();
            List<PortalInfo> portals = new List<PortalInfo>(count);
            for (int i = 0; i < count; i++)
            {
                portals.Add(new PortalInfo(pkg.ReadZDOID(), pkg.ReadVector3(), pkg.ReadString(),
                    (PortalMode)pkg.ReadInt(), pkg.ReadLong()));
            }
            PortalRegistry.SetSnapshot(portals);
        }
    }
}
