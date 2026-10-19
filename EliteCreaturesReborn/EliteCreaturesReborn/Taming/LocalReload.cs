using UnityEngine;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>
    /// Loads a creature again on this machine only, from its ZDO, exactly as the game does when a player walks out of
    /// its area and back (ZNetScene.RemoveObjects, then CreateObjects a frame or two later): the copy lets go of its ZDO
    /// and is destroyed, and the scene makes a fresh one. The ZDO is untouched - owner, health, tame, name, saddle,
    /// pregnancy - so nothing is lost; the new copy sets up its stars' look, size, health and mutations from scratch.
    /// </summary>
    internal static class LocalReload
    {
        public static void Run(ZNetView nview)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || nview == null || !nview.IsValid())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            nview.ResetZDO(); // the ZDO reads as not made, so the scene makes it again
            scene.m_instances.Remove(zdo);
            Object.Destroy(nview.gameObject);
        }
    }
}
