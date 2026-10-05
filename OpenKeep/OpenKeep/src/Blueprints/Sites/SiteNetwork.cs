using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The construction sites' routed RPCs that are not tied to a loaded post, registered on every machine when the
    /// network starts: a take-down request to the server (<see cref="SiteTakeDown.AskRpc"/>, which only the server answers,
    /// since only it knows the admin list and every player's id) and the "built" notice sent to everybody when a site is
    /// finished (each machine shows it when its player is within <see cref="CrewRange"/> metres).
    /// </summary>
    public static class SiteNetwork
    {
        public const string BuiltRpc = "OpenKeep_SiteBuilt";

        /// <summary>Players this close to a finished site are told it is built, metres.</summary>
        public const float CrewRange = 40f;

        public static void Register(ZRoutedRpc rpc)
        {
            rpc.Register<ZDOID>(SiteTakeDown.AskRpc,
                (sender, id) => BlueprintSafe.Run("OpenKeep site take down request", () => SiteTakeDown.OnAsk(sender, id)));
            rpc.Register<Vector3, string, int>(BuiltRpc,
                (sender, at, name, pieces) => BlueprintSafe.Run("OpenKeep site built", () => OnBuilt(at, name, pieces)));
        }

        /// <summary>Tells every machine a site is finished (the owner calls it just before removing the post).</summary>
        public static void AnnounceBuilt(Vector3 at, string name, int pieces)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, BuiltRpc, at, name ?? "", pieces);
        }

        private static void OnBuilt(Vector3 at, string name, int pieces)
        {
            Player player = Player.m_localPlayer;
            if (player != null && Vector3.Distance(player.transform.position, at) <= CrewRange)
                Messages.Center(BlueprintWords.Format(BlueprintWords.Done, name, pieces));
        }
    }

    /// <summary>ZNet.Awake postfix: the routed RPC table is new with every network, so the site RPCs are registered on it each time.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class SiteNetworkPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (ZRoutedRpc.instance != null)
                BlueprintSafe.Run("OpenKeep site RPCs", () => SiteNetwork.Register(ZRoutedRpc.instance));
        }
    }
}
