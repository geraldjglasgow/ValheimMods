using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Experience at kitchen crafting stations (cauldron, mead cauldron, prep table). InventoryGui.DoCrafting runs on
    /// the crafting player's client and, after a successful craft, raises the recipe station's crafting skill by 1, or
    /// by the multi-craft amount. The prefix opens an <see cref="XpScope"/> for the recipe's item while the player
    /// stands at a kitchen (Player.GetCurrentCraftingStation); crafting a dish counts as making it, for the discovery
    /// bonus. A craft that fails raises nothing, so it neither earns nor uses up the discovery.
    /// </summary>
    public static class CraftXp
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class CraftPatch
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryGui __instance, out bool __state) => __state = Open(__instance);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => XpScope.End(__state);
        }

        private static bool Open(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            ItemDrop item = gui.m_craftRecipe == null ? null : gui.m_craftRecipe.m_item;
            if (player == null || item == null || !Kitchen.IsKitchen(player.GetCurrentCraftingStation()))
                return false;
            int units = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
            return XpScope.Begin(item.m_itemData, item.gameObject.name, units);
        }
    }
}
