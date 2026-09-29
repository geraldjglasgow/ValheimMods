using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Swamp
{
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class SwampWords
    {
        public static void Add(Localization localization)
        {
            foreach (SwampKind kind in SwampKind.All)
            {
                localization.AddWord(kind.Word, kind.Name);
                for (int i = 0; i < 8; ++i) localization.AddWord(kind.Word + "_attack_" + i, kind.Name + " attack");
            }
        }

        private static void Postfix(Localization __instance) =>
            SafeCall.Run("swamp localization", () => Add(__instance));
    }
}
