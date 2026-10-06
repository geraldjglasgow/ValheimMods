using HarmonyLib;
using PatchGuard;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The crafting panel hooks of the Recipe List module. Signatures verified against the decompiled InventoryGui:
    /// Awake(), UpdateCraftingPanel(bool focusView), UpdateRecipeList(List&lt;Recipe&gt; recipes) (private, called only
    /// from UpdateCraftingPanel for the Craft and Upgrade tabs), UpdateRecipeGamepadInput() (private, from
    /// UpdateGamepad while the crafting panel is the active group), Update(), Hide(), UpdateRecipe(Player player,
    /// float dt). The Salvage tab replaces UpdateCraftingPanel's body, so the list filter never runs on it.
    /// </summary>
    [HarmonyPatch]
    public static class RecipeListPatches
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        [HarmonyPostfix]
        public static void AfterAwake(InventoryGui __instance)
        {
            SearchBar.Reset();
            TypingGuard.Install(__instance);
            Guard.Run("recipe actions", () => RecipeActions.Create(__instance));
        }

        /// <summary>First, so the search row and the list's height are right before Salvage or the game build the list.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void BeforeCraftingPanel(InventoryGui __instance) => SearchBar.Prepare(__instance);

        /// <summary>Again after a Salvage tab switch, which the Salvage module makes inside the update.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
        [HarmonyPostfix]
        public static void AfterCraftingPanel(InventoryGui __instance) => SearchBar.Prepare(__instance);

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
        [HarmonyPostfix]
        public static void AfterRecipeList(InventoryGui __instance) => RecipeFilter.Apply(__instance);

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeGamepadInput))]
        [HarmonyPrefix]
        public static bool BeforeRecipeGamepad(InventoryGui __instance) => RecipeGamepad.Handle(__instance);

        /// <summary>First, so no other prefix sees the Use or Inventory press typed into a field.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool BeforeUpdate() => TypingGuard.BeforeInventoryUpdate();

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        public static void AfterHide() => SearchBar.Closed();
    }
}
