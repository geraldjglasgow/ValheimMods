using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend's words in the game's translation table: its name and its five attacks' names (seen in the death
    /// recap and the damage log). English for every language until translations exist. Added as the creature is built
    /// and again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class BatFiendWords
    {
        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord("enemy_ecr_batfiend", "Bat-fiend");
            localization.AddWord("item_ecr_batfiend_pounce", "Raking Pounce");
            localization.AddWord("item_ecr_batfiend_bite", "Bloodletting Bite");
            localization.AddWord("item_ecr_batfiend_wing", "Wing Guillotine");
            localization.AddWord("item_ecr_batfiend_gravewind", "Gravewind Blast");
            localization.AddWord("item_ecr_batfiend_screech", "Shattering Screech");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Bat-fiend words", static l => Add(l), __instance);
    }
}
