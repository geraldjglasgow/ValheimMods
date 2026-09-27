using HarmonyLib;
using PatchGuard;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Applies Build On Wood once the scene's prefabs exist (ZNetScene.Awake, low priority so pieces other mods register
    /// in their own postfixes are found), on the server and on every client.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class FireSceneReady
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void Postfix() => Guard.Run("build on wood", FirePlacement.ApplyAll);
    }
}
