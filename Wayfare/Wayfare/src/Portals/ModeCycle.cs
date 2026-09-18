using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>Cycling a portal's access mode from its interact prompt: Alt+Use (the game's own "AltPlace"
    /// button, the same one building uses for a secondary action) instead of plain Use, which still opens the
    /// vanilla tag box. The write happens only on whichever machine owns the portal's ZDO, the same authority
    /// vanilla's own <c>RPC_SetTag</c> already uses.</summary>
    public static class ModeCycle
    {
        public const string SetModeRpc = "wf_SetMode";

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
            portal.m_nview.InvokeRPC(SetModeRpc, (int)next);
        }

        public static void OnSetMode(TeleportWorld portal, long sender, int modeRaw)
        {
            if (!WayfareConfig.Enabled.Value || portal == null || portal.m_nview == null || !portal.m_nview.IsValid() || !portal.m_nview.IsOwner())
                return;
            if (modeRaw < (int)PortalMode.Public || modeRaw > (int)PortalMode.Admin)
                return;
            ZDO zdo = portal.m_nview.GetZDO();
            long playerId = SenderIdentity.PlayerId(sender);
            bool isAdmin = SenderIdentity.IsAdmin(sender);
            if (!PortalAccess.MayCycle(zdo, playerId, isAdmin))
                return;
            PortalFields.SetModeAndOwner(zdo, (PortalMode)modeRaw, playerId);
        }

        private static PortalMode Next(PortalMode mode) => (PortalMode)(((int)mode + 1) % 3);

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
            __instance.m_nview.Register<int>(ModeCycle.SetModeRpc, (sender, mode) => ModeCycle.OnSetMode(__instance, sender, mode));
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
            __result += "\n[<color=yellow><b>$KEY_AltPlace</b></color>] " + Localization.instance.Localize(line);
        }
    }
}
