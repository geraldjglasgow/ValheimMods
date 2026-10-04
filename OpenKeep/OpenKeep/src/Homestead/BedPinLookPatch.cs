using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.UpdatePins</c> postfix: the game has just set every pin icon white (on the minimap, the large map and
    /// in <see cref="BedChoiceMap"/>); the bed icons turn yellow (<see cref="BedPinLook"/>), the nearest bed's ping gold
    /// (<see cref="BedPing"/>).
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
    public static class BedPinLookPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap __instance)
        {
            BedPinLook.Tint(__instance);
            BedPing.Tint();
        }
    }
}
