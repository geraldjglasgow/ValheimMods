using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Kraken
{
    /// <summary>
    /// The kraken's words for the game's translation table: its name, the ink's status effect and its loot. English for
    /// every language until translations exist. Added again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class KrakenWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_kraken", "Kraken");
            localization.AddWord("se_ecp_inked", "Inked");
            localization.AddWord("se_ecp_inked_tooltip", "Ink in your eyes.");
            Loot(localization);
        }

        private static void Loot(Localization localization)
        {
            localization.AddWord("item_ecp_krakenbeak", "Kraken beak");
            localization.AddWord("item_ecp_krakenbeak_description",
                "The kraken's hooked beak: dark horn with an amber edge that could shear through a hull. A smith could set it in a shield.");
            localization.AddWord("item_ecp_krakenmeat", "Raw kraken tentacle");
            localization.AddWord("item_ecp_krakenmeat_description", "A thick cut of kraken arm, suckers and all. Cook it on an iron cooking station.");
            localization.AddWord("item_ecp_krakenmeatcooked", "Cooked kraken tentacle");
            localization.AddWord("item_ecp_krakenmeatcooked_description", "Seared tentacle, rich and chewy. Keeps a crew rowing through the longest night.");
            localization.AddWord("item_ecp_shieldkraken", "Kraken shield");
            localization.AddWord("item_ecp_shieldkraken_description",
                "Fine wood and silver bound round the kraken's beak. Parry a blow with it and the beak bites back.");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage kraken words", () => Add(__instance));
    }
}
