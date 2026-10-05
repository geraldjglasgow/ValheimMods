using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>A portal looks open exactly when it is usable: when it has a tag (<see cref="PortalFields.HasTag(ZDO)"/>).
    /// The game lights a portal, plays its connect sound and shows its swirl from its tag pairing (<c>HaveTarget</c>,
    /// <c>TargetFound</c>), which pairs two untagged portals as readily as two named ones and leaves a lone named portal
    /// dark; with Wayfare any tagged portal reaches any other, so both answer from the tag instead. The game's own
    /// pairing keeps running and is never read (PLAN.md); its "connected" hover word follows the tag too.</summary>
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
        public static void Answer(TeleportWorld portal, ref bool open)
        {
            if (!WayfareConfig.Enabled.Value || portal == null || portal.m_nview == null)
                return;
            open = PortalFields.HasTag(portal.m_nview.GetZDO());
        }
    }
}
