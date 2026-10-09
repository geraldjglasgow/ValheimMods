using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista's words for the game's translation table: the piece, its missiles, its hover and its message.
    /// English for every language until translations exist. Added again after each language setup, which rebuilds the
    /// table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class BallistaWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord(BallistaPiece.Word, "Bone Ballista");
            localization.AddWord(BallistaPiece.Word + "_description",
                "A small ballista of bones, its arms two spines. Hold it to aim and shoot; it reloads from the bone missiles "
                + "you carry, a turn of 45 degrees either way.");
            localization.AddWord(BallistaMissile.Word, "Bone Missile");
            localization.AddWord(BallistaMissile.Word + "_description",
                "A missile for the bone ballista: a long bone shaft with a fang for a head and bone plates for fletching.");
            localization.AddWord("hud_ecp_bal_hold", "Hold");
            localization.AddWord("hud_ecp_bal_letgo", "Let go");
            localization.AddWord("hud_ecp_bal_loaded", "loaded");
            localization.AddWord("hud_ecp_bal_empty", "empty");
            localization.AddWord("msg_ecp_bal_nomissiles", "No bone missiles");
            localization.AddWord("msg_ecp_bal_behind", "Stand behind it to hold it");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage bone ballista words", () => Add(__instance));
    }
}
