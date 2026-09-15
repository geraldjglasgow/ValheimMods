using HarmonyLib;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// When the scene's prefabs exist (ZNetScene.Awake, low priority so other mods' prefabs are registered) the
    /// container list is generated into a still-default OpenKeep.Containers.yml and the sizes are applied; every
    /// container that awakes afterwards gets its configured size at once (Container.Awake, after the game created
    /// its inventory from the prefab's width and height).
    /// </summary>
    public static class SceneReady
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ScenePatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Low)]
            private static void Postfix()
            {
                ContainerTemplate.GenerateIfDefault();
                ContainerSizes.ApplyAll();
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class ContainerPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance) => ContainerSizes.ApplyTo(__instance);
        }
    }
}
