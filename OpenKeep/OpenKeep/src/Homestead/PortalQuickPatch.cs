using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.UpdateTeleport(float)</c> prefix (private; the game calls it in the fixed update of a living player on
    /// its owner's client): a long jump of the local player is hurried by <see cref="PortalQuick"/> before the game
    /// adds this tick to its timer.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
    public static class PortalQuickPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, float dt)
        {
            if (__instance != Player.m_localPlayer || !__instance.m_teleporting || !__instance.m_distantTeleport)
                return;
            PortalQuick.Hurry(__instance, dt);
        }
    }
}
