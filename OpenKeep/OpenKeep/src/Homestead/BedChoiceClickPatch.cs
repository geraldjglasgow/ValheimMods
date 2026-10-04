using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.OnMapLeftClick</c> prefix: while the choice of bed is open, a click picks the bed nearest to it within
    /// its click area (<see cref="BedBubble"/>: the icon at its largest; the game's own pin click radius only while no
    /// area is drawn yet) instead of ticking a pin. A click that finds no
    /// bed does nothing. The game's own click everywhere else. First, so another mod's icon lying over a bed (Wayfare's
    /// portals, whose prefix takes a click on its icon) never takes the click while the player chooses where to wake.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    public static class BedChoiceClickPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Minimap __instance)
        {
            if (!BedChoice.Active)
                return true;
            float area = BedBubble.WorldRadius(__instance);
            float radius = area > 0f ? area : __instance.PinInteractRadius;
            BedChoice.ClickAt(__instance.ScreenToWorldPoint(ZInput.pointerPosition), radius);
            return false;
        }
    }
}
