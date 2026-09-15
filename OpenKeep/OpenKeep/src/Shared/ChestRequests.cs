using System;
using HarmonyLib;

namespace OpenKeep.Shared
{
    /// <summary>
    /// The request RPCs of SPEC 9.2, registered on every container's net view in a <c>Container.Awake</c> postfix
    /// (once per view; the game registers its own RPCs there too). Requests go to the current owner of the ZDO
    /// (<c>ZNetView.InvokeRPC(method, ...)</c> resolves it at send time), replies go back to the requesting peer.
    /// Every payload is one <c>ZPackage</c>: a request starts with the request id, the requester's player id and
    /// name (<see cref="Header"/>), a reply with the request id, yes or no, a reason word and a nested package
    /// with the kind-specific result. A handler that throws on a bad packet is logged and the packet ignored.
    /// </summary>
    public static class ChestRequests
    {
        public const string TakeRpc = "OpenKeep_Take";
        public const string PutRpc = "OpenKeep_Put";
        public const string MoveRpc = "OpenKeep_Move";
        public const string TakeAllRpc = "OpenKeep_TakeAll";
        public const string StackAllRpc = "OpenKeep_StackAll";
        public const string ReplyRpc = "OpenKeep_Reply";
        public const string TouchRpc = "OpenKeep_Touch";

        public const string NotOwner = "not owner";
        public const string Denied = "denied";
        public const string ChestFull = "chestfull";
        public const string Nothing = "nothing";
        public const string Bad = "bad";
        public const string Timeout = "timeout";

        public readonly struct Header
        {
            public readonly long Id;
            public readonly long PlayerId;
            public readonly string PlayerName;

            public Header(long id, long playerId, string playerName)
            {
                Id = id;
                PlayerId = playerId;
                PlayerName = playerName;
            }
        }

        public static ZPackage NewRequest(long id)
        {
            Player player = Player.m_localPlayer;
            ZPackage pkg = new ZPackage();
            pkg.Write(id);
            pkg.Write(player != null ? player.GetPlayerID() : 0L);
            pkg.Write(player != null ? player.GetPlayerName() : "");
            return pkg;
        }

        public static Header ReadHeader(ZPackage pkg) => new Header(pkg.ReadLong(), pkg.ReadLong(), pkg.ReadString());

        /// <summary>Sends a request to the current owner of the container's ZDO. False when the container has no valid net view.</summary>
        public static bool Send(Container container, string rpc, long id, ZPackage pkg)
        {
            ZNetView view = container != null ? container.m_nview : null;
            if (view == null || !view.IsValid())
                return false;
            Plugin.Log.LogDebug($"OpenKeep: request {id} {rpc} for {Core.ContainerScan.PrefabName(container)} sent to owner {view.GetZDO().GetOwner()}");
            view.InvokeRPC(rpc, pkg);
            return true;
        }

        public static void Reply(Container container, long peer, Header header, bool ok, string reason, ZPackage payload)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(header.Id);
            pkg.Write(ok);
            pkg.Write(reason ?? "");
            pkg.Write(payload ?? new ZPackage());
            Plugin.Log.LogDebug($"OpenKeep: reply {header.Id} to {header.PlayerName}: {(ok ? "yes" : "no, " + reason)}");
            container.m_nview.InvokeRPC(peer, ReplyRpc, pkg);
        }

        internal static void Register(Container container)
        {
            ZNetView view = container.m_nview;
            if (view == null || view.m_functions == null || view.m_functions.ContainsKey(TakeRpc.GetStableHashCode()))
                return;
            view.Register<ZPackage>(TakeRpc, (sender, pkg) => Handle(TakeRpc, container, sender, pkg, ChestOwnerHandler.Take));
            view.Register<ZPackage>(PutRpc, (sender, pkg) => Handle(PutRpc, container, sender, pkg, ChestOwnerHandler.Put));
            view.Register<ZPackage>(MoveRpc, (sender, pkg) => Handle(MoveRpc, container, sender, pkg, ChestOwnerHandler.Move));
            view.Register<ZPackage>(TakeAllRpc, (sender, pkg) => Handle(TakeAllRpc, container, sender, pkg, ChestOwnerHandler.TakeAll));
            view.Register<ZPackage>(StackAllRpc, (sender, pkg) => Handle(StackAllRpc, container, sender, pkg, ChestOwnerHandler.StackAll));
            view.Register<ZPackage>(ReplyRpc, (sender, pkg) => Handle(ReplyRpc, container, sender, pkg, ChestRequester.Receive));
            view.Register<ZPackage>(TouchRpc, (sender, pkg) => Handle(TouchRpc, container, sender, pkg, Touches.Receive));
        }

        private static void Handle(string rpc, Container container, long sender, ZPackage pkg, Action<Container, long, ZPackage> handler)
        {
            try
            {
                if (container != null && container.m_nview != null && container.m_nview.IsValid() && pkg != null)
                    handler(container, sender, pkg);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: {rpc} from peer {sender} ignored: {e.GetType().Name}: {e.Message}");
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class RegisterPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                if (__instance.m_nview != null && __instance.m_nview.GetZDO() != null)
                    Register(__instance);
            }
        }
    }
}
