using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>Cycling a portal's access mode from its interact prompt: Alt+Use (the game's own "AltPlace"
    /// button, the same one building uses for a secondary action) instead of plain Use, which still opens the
    /// vanilla tag box. The write happens only on whichever machine owns the portal's ZDO, the same authority
    /// vanilla's own <c>RPC_SetTag</c> already uses. Who asked is decided by the server, never by that owner: a
    /// client's only peer is the server, so where the owner is another client <see cref="SenderIdentity"/> would
    /// name the owner's own player, and the cycle would make the owner the portal's owner. The request goes to the
    /// server (<see cref="RequestModeRpc"/>), which resolves the player and admin rights, checks the right to cycle
    /// and writes the mode where it owns the ZDO or nobody does, else forwards it with that identity to the owner
    /// (<see cref="SetModeRpc"/>), which takes it only from the server and checks the right again on its own copy.</summary>
    public static class ModeCycle
    {
        public const string SetModeRpc = "wf_SetMode";
        public const string RequestModeRpc = "wf_RequestSetMode";

        // The instance registered on, not a bool: every new session constructs a fresh ZRoutedRpc.
        private static ZRoutedRpc registeredOn;

        public static void EnsureRegistered()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null || registeredOn == rpc)
                return;
            registeredOn = rpc;
            rpc.Register<ZDOID, int>(RequestModeRpc, OnRequestMode);
        }

        public static void TryCycle(TeleportWorld portal, Player player)
        {
            if (player == null || portal == null || portal.m_nview == null || !portal.m_nview.IsValid())
                return;
            if (!PrivateArea.CheckAccess(portal.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                return;
            }
            ZDO zdo = portal.m_nview.GetZDO();
            long playerId = player.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            if (!PortalAccess.MayCycle(zdo, playerId, isAdmin))
            {
                player.Message(MessageHud.MessageType.Center, Words.NotOwner);
                return;
            }
            PortalMode next = Next(PortalFields.GetMode(zdo, WayfareConfig.UnownedPortalsArePublic.Value));
            if (ZRoutedRpc.instance != null)
                ZRoutedRpc.instance.InvokeRoutedRPC(RequestModeRpc, zdo.m_uid, (int)next);
        }

        /// <summary>Server: a player asks to cycle a portal. The identity is the sender's, resolved here.</summary>
        private static void OnRequestMode(long sender, ZDOID portalId, int modeRaw)
        {
            if (!WayfareConfig.Enabled.Value || ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null)
                return;
            ZDO zdo = ZDOMan.instance.GetZDO(portalId);
            long playerId = PlayerOf(sender);
            bool isAdmin = SenderIdentity.IsAdmin(sender);
            if (zdo == null || !PortalDiscovery.IsPortalPrefab(zdo.GetPrefab()) || !May(zdo, playerId, isAdmin, modeRaw))
                return;
            if (zdo.IsOwner() || !zdo.HasOwner())
                PortalFields.SetModeAndOwner(zdo, (PortalMode)modeRaw, playerId);
            else
                ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), zdo.m_uid, SetModeRpc, playerId, isAdmin, modeRaw);
        }

        /// <summary>Portal owner: a cycle the server checked and forwarded with the requester's identity.</summary>
        public static void OnSetMode(TeleportWorld portal, long sender, long playerId, bool isAdmin, int modeRaw)
        {
            if (!WayfareConfig.Enabled.Value || !SenderIdentity.IsFromServer(sender))
                return;
            if (portal == null || portal.m_nview == null || !portal.m_nview.IsValid() || !portal.m_nview.IsOwner())
                return;
            ZDO zdo = portal.m_nview.GetZDO();
            if (May(zdo, playerId, isAdmin, modeRaw))
                PortalFields.SetModeAndOwner(zdo, (PortalMode)modeRaw, playerId);
        }

        private static bool May(ZDO zdo, long playerId, bool isAdmin, int modeRaw)
        {
            if (playerId == 0L || modeRaw < (int)PortalMode.Public || modeRaw > (int)PortalMode.Admin)
                return false;
            return PortalAccess.MayCycle(zdo, playerId, isAdmin);
        }

        /// <summary>The sender's player on the server: a connected peer's, or this machine's own for a call it routed to
        /// itself (a host or single player); 0 for a peer that has already left.</summary>
        private static long PlayerOf(long sender)
        {
            bool known = sender == ZRoutedRpc.instance.m_id || ZNet.instance.GetPeer(sender) != null;
            return known ? SenderIdentity.PlayerId(sender) : 0L;
        }

        private static PortalMode Next(PortalMode mode) => (PortalMode)(((int)mode + 1) % 3);

        /// <summary>The Alt+Use prompt, unlocalized, as the game's item stand writes its own: the AltPlace key and Use
        /// (Shift + E), or the gamepad's alt keys and Use. The whole prompt goes through the localizer, which turns
        /// each <c>$KEY_</c> into the bound key.</summary>
        public static string AltUseKeys
        {
            get
            {
                string alt = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? "$KEY_AltKeys" : "$KEY_AltPlace";
                return "[<color=yellow><b>" + alt + " + $KEY_Use</b></color>] ";
            }
        }

        public static string ModeLabel(PortalMode mode)
        {
            switch (mode)
            {
                case PortalMode.Private: return Words.ModePrivate;
                case PortalMode.Admin: return Words.ModeAdmin;
                default: return Words.ModePublic;
            }
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), "Awake")]
    public static class TeleportWorldAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(TeleportWorld __instance)
        {
            if (__instance == null || __instance.m_nview == null || !__instance.m_nview.IsValid())
                return;
            __instance.m_nview.Register<long, bool, int>(ModeCycle.SetModeRpc,
                (sender, playerId, isAdmin, mode) => ModeCycle.OnSetMode(__instance, sender, playerId, isAdmin, mode));
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
    public static class TeleportWorldInteractPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
        {
            if (hold || !alt || !WayfareConfig.Enabled.Value)
                return true;
            __result = true;
            ModeCycle.TryCycle(__instance, human as Player);
            return false;
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    public static class TeleportWorldHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TeleportWorld __instance, ref string __result)
        {
            if (!WayfareConfig.Enabled.Value || __instance == null || __instance.m_nview == null || !__instance.m_nview.IsValid())
                return;
            PortalMode mode = PortalFields.GetMode(__instance.m_nview.GetZDO(), WayfareConfig.UnownedPortalsArePublic.Value);
            string label = Localization.instance.Localize(ModeCycle.ModeLabel(mode));
            string line = string.Format(Localization.instance.Localize(Words.HoverCycle), label);
            __result += "\n" + Localization.instance.Localize(ModeCycle.AltUseKeys) + line;
        }
    }
}
