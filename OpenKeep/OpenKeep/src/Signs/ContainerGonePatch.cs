using HarmonyLib;

namespace OpenKeep.Signs
{
    /// <summary>
    /// A container destroyed by the game (the hammer, damage, no support) takes its sign with it: the container's
    /// owner, where the game runs <c>Container.OnDestroyed</c> to drop the items, removes the linked sign at once.
    /// A container that vanishes another way leaves a sign that the orphan check removes when it loads.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
    public static class ContainerGonePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Container __instance)
        {
            ZNetView view = __instance.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            ZDO zdo = view.GetZDO();
            if (SignLinks.SignId(zdo) != ZDOID.None && SignRemover.RemoveFrom(zdo))
                Plugin.Log.LogDebug($"OpenKeep: container {zdo.m_uid} destroyed, its sign removed");
        }
    }
}
