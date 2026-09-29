using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// The crossbowman's words for the game's translation table: its name, its crossbow's, and the players' Bone
    /// Crossbow's. English for every language until translations exist. Added again after each language setup, which
    /// rebuilds the table.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class XbowWords
    {
        public static void Add(Localization localization)
        {
            localization.AddWord("enemy_ecp_skeletoncrossbowman", "Skeleton Crossbowman");
            localization.AddWord("item_ecp_skeletoncrossbow", "Crossbow");
            localization.AddWord("item_" + XbowItem.Word, "Bone Crossbow");
            localization.AddWord("item_" + XbowItem.Word + "_description",
                "A crossbow of bones, made the way the dead make theirs: a femur for a stock, a spine for a tiller, ribs for a "
                + "prod. It hits like a club, whatever bolt it looses.");
            localization.AddWord("item_" + XbowBolts.Word, "Blunted Bone Bolt");
            localization.AddWord("item_" + XbowBolts.Word + "_description",
                "A bolt of bone with its head ground blunt and no fletching, for the bone crossbow. It breaks bones rather than "
                + "piercing them.");
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("Localization.SetupLanguage crossbowman words", () => Add(__instance));
    }
}
