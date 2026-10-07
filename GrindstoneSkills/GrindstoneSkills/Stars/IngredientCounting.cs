using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Starred eggs count for recipes. Player.HaveRequirementItems, behind every
    /// "can craft" check (the recipe list, the craft button, multi-craft and DoCrafting itself), counts each
    /// requirement at every quality from 1 to the item's m_maxQuality with Inventory.CountItems(name, quality) and keeps
    /// the largest. Eggs have a maximum quality of 1, so a starred egg (quality 2 to 4) would not count, while
    /// paying (Inventory.RemoveItem(name, amount, -1)) takes any quality. While that method runs for a normal recipe,
    /// CountItems(name, 1) for such an item counts every unit. A single-ingredient recipe pays from one quality the game
    /// picks (Player.GetFirstRequiredItem, 0 to m_maxQuality), so it keeps the game's count. The requirement display
    /// (InventoryGui.SetupRequirement) already counts every quality. Any inventory counts this way inside the scope, so
    /// a mod that adds chest contents through CountItems counts them too.
    /// </summary>
    public static class IngredientCounting
    {
        private static bool counting;

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
        private static class RequirementScope
        {
            [HarmonyPrefix]
            private static void Prefix(Recipe piece) => counting = piece != null && !piece.m_requireOnlyOneIngredient;

            [HarmonyFinalizer]
            private static void Finalizer() => counting = false;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
        private static class Count
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                if (!counting || quality != 1 || !Stars.IsStarName(name))
                    return true;
                __result = __instance.CountItems(name, -1, matchWorldLevel);
                return false;
            }
        }
    }
}
