using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.OnMapDblClick</c> prefix: no new pin (and no pin name field) from a double click while the choice of
    /// bed is open, where a click that missed the beds would otherwise start one.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapDblClick))]
    public static class BedChoiceDoubleClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix() => !BedChoice.Active;
    }
}
