using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Hud.UpdateBlackScreen</c> prefix: the game fades a dead player's screen to its black loading screen, which
    /// would cover the map and take its clicks; while the choice of bed is open the screen stays clear. Once the choice
    /// ends the game's fade runs as usual.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
    public static class BedChoiceScreenPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hud __instance)
        {
            if (!BedChoice.Active)
                return true;
            __instance.m_loadingScreen.alpha = 0f;
            __instance.m_loadingScreen.gameObject.SetActive(false);
            return false;
        }
    }
}
