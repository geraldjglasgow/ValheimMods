using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Slinger
{
    /// <summary>
    /// The slinger's words for the game's translation table: its name and its shot's. English for every language until
    /// translations exist. Added again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class SlingerWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_greydwarfslinger", "Greydwarf Slinger");
            localization.AddWord("item_ecp_slinger_shot", "Slingshot stone");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage slinger words", () => Add(__instance));
    }
}
