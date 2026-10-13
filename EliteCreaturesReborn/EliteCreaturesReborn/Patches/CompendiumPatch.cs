using EliteCreaturesReborn.Compendium;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Adds the mod's pages to the game's compendium (<see cref="CompendiumPages"/>) as it fills its list, each time it
    /// opens. Never throws: a page that fails is left out and the game's own pages show as always.
    /// </summary>
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class CompendiumPatch
    {
        private static void Postfix(TextsDialog __instance) =>
            SafeCall.Run("TextsDialog elite pages", static dialog => CompendiumPages.AddTo(dialog.m_texts), __instance);
    }
}
