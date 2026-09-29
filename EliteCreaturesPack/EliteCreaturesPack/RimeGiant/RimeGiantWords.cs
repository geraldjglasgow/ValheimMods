using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.RimeGiant
{
    /// <summary>
    /// The giant's words for the game's translation table: its name and its attacks'. English for every language until
    /// translations exist. Added again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class RimeGiantWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_rimegiant", "Rime Giant");
            localization.AddWord("item_ecp_rimegiant_sweep", "Rime fist");
            localization.AddWord("item_ecp_rimegiant_slam", "Avalanche");
            localization.AddWord("item_ecp_rimegiant_throw", "Ice boulder");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage rime giant words", () => Add(__instance));
    }
}
