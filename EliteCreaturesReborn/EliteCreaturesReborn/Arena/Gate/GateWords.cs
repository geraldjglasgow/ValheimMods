using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind's words in the game's translation table: the piece's name and description, its hover line and the
    /// message of a closed gate. English for every language until translations exist. Added as the gate is made and
    /// again after each language setup, which rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class GateWords
    {
        /// <summary>The piece's name, "Helgrind".</summary>
        public const string Piece = "piece_ecr_helgrind";

        public const string Description = Piece + "_description";

        /// <summary>The hover's line while the gate is open.</summary>
        public const string Open = "ecr_helgrind_open";

        /// <summary>The hover's line, and the centre message on walking in, while Arena is off or the hall not built.</summary>
        public const string Closed = "ecr_helgrind_closed";

        public static void Add(Localization? localization)
        {
            if (localization == null)
            {
                return;
            }
            localization.AddWord(Piece, "Helgrind");
            localization.AddWord(Description,
                "Hel's gate, which leads to her hall and her trial. Walk through it to go in; the swirl at the end of the "
                + "hall's gate passage brings you back out here.");
            localization.AddWord(Open, "Walk through into Hel's hall");
            localization.AddWord(Closed, "Hel's gate is closed.");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Helgrind words", static l => Add(l), __instance);
    }
}
