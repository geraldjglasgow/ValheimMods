using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>ZNetScene.CreateDestroyObjects()</c> postfix (private, 30 times a second on every machine): after the game's
    /// own create and remove pass, <see cref="PortalObjects"/> creates more of the listed objects while a jump or
    /// respawn waits.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateDestroyObjects))]
    public static class PortalObjectsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZNetScene __instance) => PortalObjects.Hurry(__instance);
    }
}
