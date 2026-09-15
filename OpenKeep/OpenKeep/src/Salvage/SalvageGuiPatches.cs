using HarmonyLib;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The crafting panel hooks. While the Salvage tab is active the panel update, the craft button and the
    /// gamepad list stepping are taken over; the game's own tabs are untouched otherwise. Signatures verified
    /// against the decompiled InventoryGui: Awake(), UpdateCraftingPanel(bool focusView), UpdateRecipe(Player
    /// player, float dt), OnCraftPressed(), OnTabCraftPressed(), OnTabUpgradePressed(), UpdateRecipeGamepadInput().
    /// </summary>
    [HarmonyPatch]
    public static class SalvageGuiPatches
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        [HarmonyPostfix]
        public static void AfterAwake(InventoryGui __instance) => SalvageTab.Create(__instance);

        /// <summary>Takes over the panel while the tab is active: our list instead of recipes, the game's tab visibility rules.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
        [HarmonyPrefix]
        public static bool BeforeCraftingPanel(InventoryGui __instance, bool focusView)
        {
            if (!SalvageTab.Active)
                return true;
            Player player = Player.m_localPlayer;
            if (!SalvageSettings.Enabled.Value || player == null)
            {
                SalvageTab.ForceCraftTab(__instance);
                return true;
            }
            SalvageTab.ShowGameTabs(__instance, player);
            SalvageList.Rebuild(__instance, focusView);
            return false;
        }

        /// <summary>Runs after both paths: the tab's visibility, label and place follow the game's tabs.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
        [HarmonyPostfix]
        public static void AfterCraftingPanel(InventoryGui __instance) => SalvageTab.Refresh(__instance);

        /// <summary>The game has just cleared the right side (no recipe selected); fill it with the selected stack.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        [HarmonyPostfix]
        public static void AfterUpdateRecipe(InventoryGui __instance, Player player)
        {
            if (SalvageTab.Active)
                SalvagePanel.Fill(__instance, player);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
        [HarmonyPrefix]
        public static bool BeforeCraftPressed(InventoryGui __instance)
        {
            if (!SalvageTab.Active)
                return true;
            SalvageList.SalvageSelected(__instance);
            return false;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
        [HarmonyPrefix]
        public static void BeforeTabCraft() => SalvageTab.Deselect();

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
        [HarmonyPrefix]
        public static void BeforeTabUpgrade() => SalvageTab.Deselect();

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeGamepadInput))]
        [HarmonyPrefix]
        public static bool BeforeRecipeGamepadInput(InventoryGui __instance)
        {
            if (!SalvageTab.Active)
                return true;
            if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
                SalvageList.Step(__instance, 1);
            if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
                SalvageList.Step(__instance, -1);
            return false;
        }
    }
}
