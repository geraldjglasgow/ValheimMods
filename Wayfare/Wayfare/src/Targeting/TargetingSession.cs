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

        /// <summary>The portal the player stands at, the map's "you are here"; none outside a session.</summary>
        public static ZDOID SourceId
        {
            get
            {
                if (!Active || SourcePortal == null || SourcePortal.m_nview == null || !SourcePortal.m_nview.IsValid())
                    return ZDOID.None;
                return SourcePortal.m_nview.GetZDO().m_uid;
            }
        }

        public static void Open(TeleportWorld source)
        {
            if (source == null || Minimap.instance == null || Player.m_localPlayer == null || !WayfareConfig.PortalsOn)
                return;
            SourcePortal = source;
            Active = true;
            PortalRegistry.Refresh(); // a portal built since the last check shows at once (a host frames it too)
            MapOpening.Large(Minimap.instance);
            MapFit.FitPortals(Minimap.instance, Player.m_localPlayer, source.transform.position);
        }

        public static void Close()
        {
            Active = false;
            SourcePortal = null;
        }

        public static void Select(ZDOID target)
        {
            ZDOID source = SourceId;
            if (source == ZDOID.None || target == source)
                return;
            TeleportGate.RequestTeleport(source, target);
        }

        /// <summary>The server's grant for the map's session (<see cref="PortalTravel"/>).</summary>
        public static void CompleteTeleport(Vector3 targetPos, Quaternion targetRot)
        {
            if (Player.m_localPlayer == null || SourcePortal == null)
            {
                Close();
                return;
            }
            if (PortalTravel.Go(SourcePortal, targetPos, targetRot) && Minimap.instance != null)
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
        }

        public static void Deny(string localizedReasonToken)
        {
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, localizedReasonToken);
        }
    }

    /// <summary>Vanilla's only call site for <c>TeleportWorld.Teleport</c>: the local player's collider entering a
    /// portal's trigger. Replaced with opening targeting instead of an immediate tag-paired teleport: the map, tagged
    /// or not (the tag is only the portal's name), or the TargetTeleport picker. The Default teleport mode keeps the
    /// game's own teleport.</summary>
    [HarmonyPatch(typeof(TeleportWorldTrigger), "OnTriggerEnter")]
    public static class PortalTriggerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TeleportWorldTrigger __instance, Collider colliderIn)
        {
            if (!WayfareConfig.PortalsOn)
                return true;
            Player player = colliderIn != null ? colliderIn.GetComponent<Player>() : null;
            if (player == null || player != Player.m_localPlayer)
                return true;
            TeleportWorld portal = __instance.GetComponentInParent<TeleportWorld>();
            if (WayfareConfig.InMode(TeleportMode.TargetTeleport))
                PortalPicker.Open(portal);
            else
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
        public static void Postfix()
        {
            TargetingSession.Close();
            PortalPicker.Close();
        }
    }
}
