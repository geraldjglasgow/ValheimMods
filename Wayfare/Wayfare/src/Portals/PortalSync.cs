using System.Collections.Generic;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>Server-to-client portal snapshots. The game only ever sends portal ZDOs for sectors near each
    /// peer (<c>ZDOMan.CreateSyncList</c> → <c>FindSectorObjects</c>), so a pure client's own
    /// <c>ZDOMan.GetPortalList()</c> holds just the portals it has happened to be near this session - nearly
    /// empty right after login. The server is the one machine whose list is complete (the portal chunk is always
    /// loaded there), so clients ask it on the registry's own 5-second cadence instead of reading their local,
    /// near-only list. Each request carries the version of the list the client already holds
    /// (<see cref="PortalListVersion"/>), and the server answers only when its list differs: an unchanged list is
    /// never built or sent again, so the ask costs the server a pass over its portal ZDOs' revision numbers and the
    /// network a few bytes.</summary>
    public static class PortalSync
    {
        public const string RequestRpc = "wf_RequestPortals";
        public const string ListRpc = "wf_PortalList";

        // The instance registered on, not a bool: every new session constructs a fresh ZRoutedRpc with empty
        // handler tables, and an unknown routed method is dropped silently.
        private static ZRoutedRpc registeredOn;

        // Client: the version of the server's list the registry's snapshot is; 0 while none is held. A server without
        // versions (an older Wayfare) sends none and every list again, as before.
        private static long held;

        // Server: the last list built, sent as is to every client that asks while it is current.
        private static long builtVersion;
        private static ZPackage built;

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            held = 0L;
            built = null;
            rpc.Register<long>(RequestRpc, OnRequestPortals);
            rpc.Register<ZPackage>(ListRpc, OnPortalList);
        }

        /// <summary>Client side: ask the server for its list, unless the one held is still current. No explicit target
        /// routes to the server.</summary>
        public static void RequestFromServer()
        {
            EnsureRegistered();
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, held);
        }

        private static void OnRequestPortals(long sender, long senderHolds)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !WayfareConfig.Enabled.Value || ZDOMan.instance == null)
                return;
            PortalDiscovery.EnsureDiscovered();
            long version = PortalListVersion.Read();
            if (version == senderHolds)
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ListRpc, Snapshot(version));
        }

        /// <summary>The list for this version, built once however many clients ask for it.</summary>
        private static ZPackage Snapshot(long version)
        {
            if (built == null || builtVersion != version)
            {
                built = BuildSnapshot(version);
                builtVersion = version;
            }
            return built;
        }

        /// <summary>The list, then its version, which a client from before versions never reads.</summary>
        private static ZPackage BuildSnapshot(long version)
        {
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
            pkg.Write(version);
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
            held = pkg.GetPos() < pkg.Size() ? pkg.ReadLong() : 0L;
            PortalRegistry.SetSnapshot(portals);
        }
    }

    /// <summary>The server's portal list as one number (<see cref="ListHash"/>): every portal ZDO's id, data revision and
    /// position, in the game's own order. A ZDO's data revision goes up with every change to its data (a tag, Wayfare's
    /// mode or owner), on the server too (it takes the owner's revision with the data), so a rename, a mode change, a
    /// moved, new or removed portal all give another number. Read straight from the game's portal lists: no copy, no
    /// string, no per-key lookup.</summary>
    internal static class PortalListVersion
    {
        internal static long Read()
        {
            Dictionary<ZoneSystem.SectorIndex, List<ZDO>> sectors = ZDOMan.instance.GetPortals();
            ListHash hash = ListHash.Start(sectors.Count);
            foreach (List<ZDO> portals in sectors.Values)
            {
                hash.Add(portals.Count);
                foreach (ZDO zdo in portals)
                {
                    hash.Add(zdo.m_uid.UserID);
                    hash.Add(zdo.m_uid.ID);
                    hash.Add(zdo.DataRevision);
                    hash.Add(zdo.GetPosition());
                }
            }
            return hash.Value;
        }
    }
}
