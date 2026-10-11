using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The Raiders Chest's words in the game's translation table: the piece's name and description, Shift + E's action and
    /// the horn's caption. English for every language until translations exist. Added as the chest is made and again after
    /// each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class ChestWords
    {
        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord(ChestPrefab.Word, "Raiders Chest");
            localization.AddWord(ChestPrefab.Word + "_description",
                "An iron-bound chest with a war horn on its lid. Fill it with gold and sound the horn: raiders come for the "
                + "gold, and beating them brings it back with more. The more gold for your gear, the harder the raid and the "
                + "richer the loot. Only in a base, once a day.");
            localization.AddWord(ChestText.SoundWord, "Sound the raid");
            localization.AddWord(RaidHorn.CaptionWord, "War horn");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Raiders Chest words", static l => Add(l), __instance);
    }
}
