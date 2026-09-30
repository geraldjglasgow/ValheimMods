using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The Crypt Executioner's words for the game's translation table: its name, its attacks' (never shown), and the
    /// players' greataxe and axehead. English for every language until translations exist. Added again after each
    /// language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class HeadsmanWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_headsman", "Crypt Executioner");
            foreach (string attack in new[] { "slam", "scrape", "spin", "hurl", "spinthrow", "rear" })
            {
                localization.AddWord("item_ecp_headsman_" + attack, "Executioner's axe");
            }
            localization.AddWord("item_" + GreataxeItems.AxeWord, "Executioner's Greataxe");
            localization.AddWord("item_" + GreataxeItems.AxeWord + "_description",
                "The Crypt Executioner's greataxe, its haft strung from vertebrae. It slashes, whirls all the way round, then "
                + "falls from overhead; the last blow is slow to recover from.");
            localization.AddWord("item_" + GreataxeItems.HeadWord, "Executioner's axehead");
            localization.AddWord("item_" + GreataxeItems.HeadWord + "_description",
                "The bone head of the Crypt Executioner's axe. Set on a haft of spines at the workbench, it swings again.");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage crypt executioner words", () => Add(__instance));
    }
}
