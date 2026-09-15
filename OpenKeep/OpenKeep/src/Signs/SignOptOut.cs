using HarmonyLib;

namespace OpenKeep.Signs
{
    /// <summary>
    /// Removing an automatic sign with the hammer means "no sign here": the removing player's client, where the game
    /// runs <c>WearNTear.Remove</c> before asking the piece's owner to destroy it, sets <c>OpenKeep.noSign</c> on the
    /// container, and the container gets no new sign until <c>openkeep signs reset</c>. A sign destroyed any other
    /// way (a troll) is simply placed again on the container's next tick; the mod's own removals go through the
    /// scene and never mark a container.
    /// </summary>
    public static class SignOptOut
    {
        public static void MarkContainer(ZDO sign)
        {
            if (!SignLinks.IsAutomatic(sign) || ZDOMan.instance == null)
                return;
            ZDOID containerId = SignLinks.ContainerId(sign);
            ZDO container = ZDOMan.instance.GetZDO(containerId);
            if (container == null || SignLinks.NoSign(container))
                return;
            if (SignLinks.InUseByAnother(container) || !SignLinks.Claim(container))
            {
                Plugin.Log.LogWarning($"OpenKeep: sign removed with the hammer but container {containerId} is in use elsewhere; it may get a new sign");
                return;
            }
            SignLinks.SetNoSign(container, true);
            Plugin.Log.LogInfo($"OpenKeep: sign removed with the hammer; container {containerId} gets no new sign (openkeep signs reset allows it again)");
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
        private static class RemovePatch
        {
            [HarmonyPostfix]
            private static void Postfix(WearNTear __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view != null && view.IsValid())
                    MarkContainer(view.GetZDO());
            }
        }
    }
}
