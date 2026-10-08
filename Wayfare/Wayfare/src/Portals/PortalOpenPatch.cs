using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>Every portal looks open: with Wayfare any portal reaches any other, tagged or not, and the tag is only
    /// its name (the user's rule, 2026-10-05); in the TargetTeleport mode every named portal (the user's rule,
    /// 2026-10-08). The game lights a portal, plays its connect sound and shows its swirl
    /// from its tag pairing (<c>HaveTarget</c>, <c>TargetFound</c>), which leaves a lone or untagged portal dark, so
    /// both answer true instead. The game's own pairing keeps running and is never read (PLAN.md); its "connected"
    /// hover word follows this too.</summary>
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.HaveTarget))]
    public static class PortalHaveTargetPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TeleportWorld __instance, ref bool __result) => PortalOpen.Answer(__instance, ref __result);
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.TargetFound))]
    public static class PortalTargetFoundPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TeleportWorld __instance, ref bool __result) => PortalOpen.Answer(__instance, ref __result);
    }

    public static class PortalOpen
    {
        /// <summary>Map: every portal is open. TargetTeleport: a named portal is open, an unnamed one dark (only named
        /// portals are connected there). Default: the game's own answer.</summary>
        public static void Answer(TeleportWorld portal, ref bool open)
        {
            if (!WayfareConfig.PortalsOn || portal == null || portal.m_nview == null || !portal.m_nview.IsValid())
                return;
            open = !WayfareConfig.InMode(TeleportMode.TargetTeleport) || PortalFields.HasName(portal.m_nview.GetZDO());
        }
    }
}
