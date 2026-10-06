using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Tells a sender why the owner (or the server) refused its edit: a routed RPC carrying the reason, shown as a
    /// centre message at most once a second so a held stroke does not flood the screen.
    /// </summary>
    public static class Refusals
    {
        public const string RefusedRpc = "EW_EditRefused";

        public static string NotAdmin { get; private set; } = "$ew_refused_notadmin";

        private static ZRoutedRpc registeredOn;
        private static float lastShown = -10f;

        internal static void Initialize()
        {
            NotAdmin = Language.Add("ew_refused_notadmin", "Only an admin can do that");
        }

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            rpc.Register<string>(RefusedRpc, (sender, reason) => Show(reason));
        }

        /// <summary>Sends the reason to the peer; shown locally when the peer is this machine.</summary>
        public static void Send(long peer, string reason)
        {
            if (peer == 0L || peer == ZNet.GetUID() || ZRoutedRpc.instance == null)
            {
                Show(reason);
                return;
            }
            EnsureRegistered();
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, RefusedRpc, reason);
        }

        /// <summary>Shows a reason on this machine (at most once a second).</summary>
        internal static void Show(string reason)
        {
            if (Time.time - lastShown < 1f)
                return;
            lastShown = Time.time;
            Messages.Center(reason);
        }
    }
}
