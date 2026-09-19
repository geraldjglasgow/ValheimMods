using HarmonyLib;
using UnityEngine;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The state of one targeting session: the portal the player walked into, and whether the large map
    /// is currently showing targeting icons because of it. Every entry point guards
    /// <c>Minimap.instance</c>/<c>Player.m_localPlayer</c> first - see SPEC's stability requirement, this is
    /// exactly the code path the incident it describes ran through.</summary>
    public static class TargetingSession
    {
        public static bool Active { get; private set; }
        public static TeleportWorld SourcePortal { get; private set; }

        public static void Open(TeleportWorld source)
        {
            if (source == null || Minimap.instance == null || Player.m_localPlayer == null || !WayfareConfig.Enabled.Value)
                return;
            SourcePortal = source;
            Active = true;
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
        }

        public static void Close()
        {
            Active = false;
            SourcePortal = null;
        }

        public static void Select(ZDOID target)
        {
            if (!Active || SourcePortal == null || SourcePortal.m_nview == null || !SourcePortal.m_nview.IsValid())
                return;
            TeleportGate.RequestTeleport(SourcePortal.m_nview.GetZDO().m_uid, target);
        }

        /// <summary>The destination comes from the server's grant, never from a local ZDO lookup - a client
        /// usually holds no ZDO at all for a distant portal.</summary>
        public static void CompleteTeleport(Vector3 targetPos, Quaternion targetRot)
        {
            if (Player.m_localPlayer == null || SourcePortal == null)
            {
                Close();
                return;
            }
            if (!Player.m_localPlayer.IsTeleportable(SourcePortal.m_allowAllItems))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
            Vector3 exitPos = targetPos + targetRot * Vector3.forward * SourcePortal.m_exitDistance + Vector3.up;
            Player.m_localPlayer.TeleportTo(exitPos, targetRot, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
            if (Minimap.instance != null)
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }

        public static void Deny(string localizedReasonToken)
        {
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, localizedReasonToken);
        }
    }

    /// <summary>Vanilla's only call site for <c>TeleportWorld.Teleport</c>: the local player's collider entering a
    /// portal's trigger. Replaced with opening targeting instead of an immediate tag-paired teleport.</summary>
    [HarmonyPatch(typeof(TeleportWorldTrigger), "OnTriggerEnter")]
    public static class PortalTriggerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TeleportWorldTrigger __instance, Collider colliderIn)
        {
            if (!WayfareConfig.Enabled.Value)
                return true;
            Player player = colliderIn != null ? colliderIn.GetComponent<Player>() : null;
            if (player == null || player != Player.m_localPlayer)
                return true;
            TeleportWorld portal = __instance.GetComponentInParent<TeleportWorld>();
            TargetingSession.Open(portal);
            return false;
        }
    }

    /// <summary>Leaving the large map (however it happened - Escape, the map's own close button, opening the
    /// inventory) cancels an active targeting session with no teleport.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapMode))]
    public static class MapModeChangePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap.MapMode mode)
        {
            if (TargetingSession.Active && mode != Minimap.MapMode.Large)
                TargetingSession.Close();
        }
    }

    /// <summary>A world unload (disconnect, quit to menu, connection loss) tears down <c>Minimap</c> without
    /// necessarily running <see cref="MapModeChangePatch"/> first - without this, a session left open across that
    /// teardown would stay <see cref="TargetingSession.Active"/> into whatever loads next, and every guard above
    /// that assumes "Active implies Minimap.instance exists" would no longer hold.</summary>
    [HarmonyPatch(typeof(Game), "OnDestroy")]
    public static class GameTeardownPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => TargetingSession.Close();
    }
}
