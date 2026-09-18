using HarmonyLib;
using UnityEngine;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The one place a teleport is granted or refused. The requesting client always sends to the server
    /// (<c>ZRoutedRpc.InvokeRoutedRPC</c> with no explicit target routes to it), never to the target portal's own
    /// ZDO owner - a portal is usually owned by some other player's machine, and trusting that machine to police
    /// every other player's access would let a malicious owner wave through (or block) requests on its own say-so.
    /// The server is the one party both sides already trust for the whole session.</summary>
    public static class TeleportGate
    {
        public const string RequestRpc = "wf_RequestTeleport";
        public const string GrantedRpc = "wf_TeleportGranted";
        public const string DeniedRpc = "wf_TeleportDenied";

        private static bool registered;

        public static void EnsureRegistered()
        {
            if (registered || ZRoutedRpc.instance == null)
                return;
            registered = true;
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RequestRpc, OnRequestTeleport);
            ZRoutedRpc.instance.Register<ZDOID>(GrantedRpc, OnGranted);
            ZRoutedRpc.instance.Register<ZDOID, string>(DeniedRpc, OnDenied);
        }

        public static void RequestTeleport(ZDOID source, ZDOID target)
        {
            EnsureRegistered();
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestRpc, source, target);
        }

        private static void OnRequestTeleport(long sender, ZDOID sourceId, ZDOID targetId)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return;
            string denial = Evaluate(sender, sourceId, targetId);
            if (denial != null)
                Deny(sender, targetId, denial);
            else
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, GrantedRpc, targetId);
        }

        /// <summary>Null when the request is granted, otherwise the localised denial reason token to send back.</summary>
        private static string Evaluate(long sender, ZDOID sourceId, ZDOID targetId)
        {
            if (!WayfareConfig.Enabled.Value)
                return Words.DeniedGeneric;
            PortalDiscovery.EnsureDiscovered();
            if (Blocked(out string blockedReason))
                return blockedReason;
            ZDO sourceZdo = ZDOMan.instance.GetZDO(sourceId);
            ZDO targetZdo = ZDOMan.instance.GetZDO(targetId);
            if (sourceZdo == null || targetZdo == null || !PortalDiscovery.IsPortalPrefab(targetZdo.GetPrefab()))
                return Words.DeniedGeneric;
            bool unownedIsPublic = WayfareConfig.UnownedPortalsArePublic.Value;
            if (!PortalAccess.MayTarget(targetZdo, SenderIdentity.PlayerId(sender), SenderIdentity.IsAdmin(sender), unownedIsPublic))
                return DenialReason(targetZdo, unownedIsPublic);
            return null;
        }

        private static bool Blocked(out string reason)
        {
            reason = Words.DeniedBlocked;
            ZoneSystem zones = ZoneSystem.instance;
            if (zones == null)
                return false;
            if (zones.GetGlobalKey(GlobalKeys.NoPortals))
                return true;
            bool activeBossEvent = RandEventSystem.instance != null && RandEventSystem.instance.GetBossEvent() != null;
            bool activeBossKey = zones.GetGlobalKey(GlobalKeys.NoBossPortals) &&
                (activeBossEvent || (zones.GetGlobalKey(GlobalKeys.activeBosses, out float count) && count > 0f));
            return activeBossKey;
        }

        private static string DenialReason(ZDO targetZdo, bool unownedIsPublic)
        {
            return PortalFields.GetMode(targetZdo, unownedIsPublic) == PortalMode.Admin ? Words.DeniedAdmin : Words.DeniedPrivate;
        }

        private static void Deny(long sender, ZDOID targetId, string reasonToken)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, DeniedRpc, targetId, reasonToken);
        }

        private static void OnGranted(long sender, ZDOID targetId) => TargetingSession.CompleteTeleport(targetId);

        private static void OnDenied(long sender, ZDOID targetId, string reasonToken) => TargetingSession.Deny(reasonToken);
    }

    /// <summary>Registers the moment <c>ZRoutedRpc</c> exists, on every machine including a headless dedicated
    /// server (which never runs the portal registry's tick, the other place this mod is active) - the server must
    /// have its request handler registered before the first client can possibly send a request.</summary>
    [HarmonyPatch(typeof(ZRoutedRpc))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(bool) })]
    public static class ZRoutedRpcConstructedPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => TeleportGate.EnsureRegistered();
    }
}
