using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>ZoneSystem.Update()</c> postfix (private, every frame on every machine): after the game's own zone step,
    /// <see cref="PortalLoad"/> loads the rest of the area at once while a jump or respawn waits for it.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Update))]
    public static class PortalLoadPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ZoneSystem __instance) => PortalLoad.Tick(__instance);
    }
}
