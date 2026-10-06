using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Applies Build On Wood once the scene's prefabs exist (ZNetScene.Awake, low priority so pieces other mods register
    /// in their own postfixes are found), on the server and on every client. Never throws into the scene's Awake
    /// (<see cref="SceneSafe"/>).
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class FireSceneReady
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void Postfix() => SceneSafe.Run("build on wood", FirePlacement.ApplyAll);
    }
}
