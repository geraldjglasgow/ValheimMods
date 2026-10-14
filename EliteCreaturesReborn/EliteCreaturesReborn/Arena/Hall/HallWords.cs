using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The hall's words in the game's translation table: Hel's name, E's action on her, and a death in her river.
    /// English for every language until translations exist. Added as the hall's host wakes and again after each language setup, which rebuilds
    /// the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class HallWords
    {
        /// <summary>Hel's name on her hover.</summary>
        public const string Hel = "ecr_arena_hel";

        /// <summary>E's action on Hel, "Begin the trial".</summary>
        public const string Begin = "hud_ecr_arena_begin";

        /// <summary>The centre message of a death in Hel's river, "Hel's river takes you."</summary>
        public const string River = "msg_ecr_arena_river";

        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord(Hel, "Hel");
            localization.AddWord(Begin, "Begin the trial");
            localization.AddWord(River, "Hel's river takes you.");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Hel's hall words", static l => Add(l), __instance);
    }
}
