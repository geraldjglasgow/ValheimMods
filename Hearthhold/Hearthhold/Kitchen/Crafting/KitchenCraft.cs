using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Crafting at a kitchen crafting station (cauldron, mead cauldron, prep table). InventoryGui.DoCrafting runs on the
    /// crafting player's client: it checks the requirements, rolls the game's bonus food, adds the whole amount with one
    /// Inventory.AddItem at quality 1, pays the ingredients, then raises the skill. Upgrades take their own branch and
    /// are left alone. The prefix captures the craft (<see cref="KitchenCraftContext"/>), puts the crafter's best
    /// ingredients first (<see cref="KitchenIngredientOrder"/>) and starts recording what is used up
    /// (<see cref="KitchenIngredientRecord"/>). The postfix gives every crafted unit its stars
    /// (<see cref="KitchenCraftSplit"/>). It runs after GrindstoneSkills' postfix, whose ingredient save reads its own
    /// record of removals until then: taking the starred units back out must not look to it like ingredients used up.
    /// The finalizer puts the inventory order back and clears everything, also when the craft failed or threw.
    /// </summary>
    public static class KitchenCraft
    {
        private static KitchenCraftContext current;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class Crafting
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryGui __instance, Player player) =>
                HookGuard.Run("kitchen craft", static args => Begin(args.Item1, args.Item2), (__instance, player));

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            [HarmonyAfter(GrindstoneLink.Guid)]
            private static void Postfix()
            {
                if (current != null)
                    HookGuard.Run("kitchen craft stars", Finish);
            }

            [HarmonyFinalizer]
            private static void Finalizer() => HookGuard.Run("kitchen craft", End);
        }

        private static void Begin(InventoryGui gui, Player player)
        {
            End();
            CraftingStation station = player == null ? null : player.GetCurrentCraftingStation();
            Recipe recipe = gui.m_craftRecipe;
            if (player == null || player != Player.m_localPlayer || !Kitchen.IsKitchen(station) || recipe?.m_item == null || gui.m_craftUpgradeItem != null)
                return;
            current = new KitchenCraftContext(gui, player, station);
            KitchenIngredientOrder.BestFirst(player.GetInventory());
            KitchenIngredientRecord.Start();
        }

        private static void Finish()
        {
            KitchenCraftContext craft = current;
            KitchenIngredientRecord.Stop();
            if (craft != null && craft.Grades)
                KitchenCraftSplit.Apply(craft);
        }

        private static void End()
        {
            KitchenIngredientRecord.Clear();
            KitchenIngredientOrder.Restore();
            current = null;
        }
    }
}
