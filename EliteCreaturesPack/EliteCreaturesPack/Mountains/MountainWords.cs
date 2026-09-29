using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Mountains
{
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    public static class MountainWords
    {
        public static void Add(Localization localization)
        {
            foreach (MountainKind kind in MountainKind.All)
            {
                localization.AddWord(kind.Word, kind.Name);
                localization.AddWord("item_ecp_" + kind.Id.ToLowerInvariant() + "_attack", kind.Name + " attack");
            }
        }
        private static void Postfix(Localization __instance) => SafeCall.Run("mountain localization", () => Add(__instance));
    }
}
