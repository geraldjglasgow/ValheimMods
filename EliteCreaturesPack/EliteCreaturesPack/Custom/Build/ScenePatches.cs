using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>A world's network scene woke: a new session for the custom creatures (<see cref="BuildTiming.SceneWoke"/>).</summary>
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class CustomSceneWakePatch
    {
        private static void Postfix(ZNetScene __instance) =>
            SafeCall.Run("custom creatures scene", static scene => BuildTiming.SceneWoke(scene), __instance);
    }

    /// <summary>
    /// Every Awake of the world's scene has run and no object is made yet: the custom creatures are built here on the
    /// server side (<see cref="BuildTiming.SceneReady"/>). Last among the postfixes, so other mods' late registrations
    /// are in.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class CustomSceneReadyPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => SafeCall.Run("custom creatures build", BuildTiming.SceneReady);
    }
}
