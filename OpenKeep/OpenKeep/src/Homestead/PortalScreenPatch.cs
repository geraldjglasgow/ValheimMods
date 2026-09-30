using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Hud.UpdateBlackScreen(Player, float)</c> prefix: during a jump into an area that was already loaded
    /// (<see cref="PortalScreen"/>) the screen does what the game does when nothing holds it, fade out, instead of
    /// fading to black with the teleport swirl. A quick jump that loads goes black faster, then the game's update runs.
    /// Every other case is the game's.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
    public static class PortalScreenPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hud __instance, Player player, float dt)
        {
            if (PortalScreen.Clear(player))
            {
                PortalScreen.FadeOut(__instance, dt);
                return false;
            }
            if (PortalScreen.Loading(player))
                PortalScreen.FadeInFast(__instance, dt);
            return true;
        }
    }
}
