using HarmonyLib;
using EliteCreaturesPack.Core;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// The mimic's words for the game's translation table: its name once it reveals itself, and its bite's. English for
    /// every language until translations exist. Added again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class MimicWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_cryptmimic", "Crypt Mimic");
            localization.AddWord("item_ecp_cryptmimic_bite", "Mimic bite");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage mimic words", () => Add(__instance));
    }
}
