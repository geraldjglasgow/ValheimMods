using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.UpdateProfilePins</c> postfix: the game has placed its own spawn point icon (every frame, in the map
    /// update on the player's client, and in <see cref="BedChoiceMap"/> during the choice); the other beds follow.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateProfilePins))]
    public static class BedPinsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap __instance) => BedPins.Refresh(__instance);
    }
}
