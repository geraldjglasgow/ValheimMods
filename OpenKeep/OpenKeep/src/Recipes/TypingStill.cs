using HarmonyLib;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The player stands still while typing in OpenKeep's fields (the recipe search, the batch amount, Mímir's Chest's
    /// search): the game lets the player walk with the inventory open and reads W, A, S and D through
    /// PlayerController.TakeInput, which already refuses for the game's own text fields (chat, console, sign text, the
    /// build menu's search) but knows nothing of a mod's. Typing "wood" walked the player forward (user, 2026-10-07).
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    public static class TypingStill
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (__result && TypingGuard.TypingNow)
                __result = false;
        }
    }
}
