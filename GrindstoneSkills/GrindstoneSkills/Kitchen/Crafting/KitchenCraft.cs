using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crafting at a kitchen crafting station (cauldron, mead cauldron, prep table). InventoryGui.DoCrafting runs on
    /// the crafting player's client: it rolls the game's bonus food (m_craftBonusChance times the skill factor, for
    /// stackable items), adds the whole amount, multi-craft and bonus included, with one Inventory.AddItem at quality
    /// 1, pays the ingredients, then raises the skill. Upgrades take their own branch and are left alone.
    /// The prefix swaps in GrindstoneSkills' extra food chance (<see cref="ExtraFood"/>), captures the craft
    /// (<see cref="KitchenCraftContext"/>) and starts recording the ingredients (<see cref="CraftRecord"/>). The postfix
    /// gives every crafted unit its star (<see cref="CraftSplit"/>) and may give an ingredient back
    /// (<see cref="IngredientSave"/>). The finalizer puts everything back, also when the craft failed or threw.
    /// All of it is local to the crafter; only the station's trash filter is read from its ZDO.
    /// </summary>
    public static class KitchenCraft
    {
        private static KitchenCraftContext current;
        private static ExtraFood.State extraFood;

        /// <summary>The kitchen craft DoCrafting is running, or null.</summary>
        public static KitchenCraftContext Current => current;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class Crafting
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryGui __instance, Player player) => Begin(__instance, player);

            [HarmonyPostfix]
            private static void Postfix() => Finish();

            [HarmonyFinalizer]
            private static void Finalizer() => End();
        }

        private static void Begin(InventoryGui gui, Player player)
        {
            End();
            CraftingStation station = Player.m_localPlayer == null ? null : Player.m_localPlayer.GetCurrentCraftingStation();
            Recipe recipe = gui.m_craftRecipe;
            if (player == null || !Kitchen.IsKitchen(station) || recipe == null || recipe.m_item == null || gui.m_craftUpgradeItem != null)
                return;
            current = new KitchenCraftContext(gui, player, station);
            extraFood = ExtraFood.Apply();
            CraftRecord.Start();
        }

        private static void Finish()
        {
            KitchenCraftContext craft = current;
            if (craft == null)
                return;
            CraftRecord.Stop();
            if (craft.Grades)
                CraftSplit.Apply(craft, CraftRecord.AverageStars());
            IngredientSave.Roll(craft, CraftRecord.Lots);
        }

        private static void End()
        {
            ExtraFood.Restore(extraFood);
            extraFood = default;
            CraftRecord.Clear();
            current = null;
        }
    }
}
