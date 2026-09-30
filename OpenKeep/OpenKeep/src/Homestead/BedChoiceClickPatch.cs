using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.OnMapLeftClick</c> prefix: while the choice of bed is open, a click picks the bed nearest to it within
    /// the game's own pin click radius (which grows as the map zooms out) instead of ticking a pin. A click that finds no
    /// bed does nothing. The game's own click everywhere else.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    public static class BedChoiceClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Minimap __instance)
        {
            if (!BedChoice.Active)
                return true;
            BedChoice.ClickAt(__instance.ScreenToWorldPoint(ZInput.pointerPosition), __instance.PinInteractRadius);
            return false;
        }
    }
}
