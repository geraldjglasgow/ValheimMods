namespace GrindstoneSkills
{
    /// <summary>
    /// One craft at a kitchen crafting station, captured before InventoryGui.DoCrafting runs: what is made, where, by
    /// whom, how many batches (1, or the multi-craft amount), the crafter's 0-star count of the item, and the
    /// variant and cheated flag the game gives the crafted items. The cheated flag is worked out as DoCrafting does
    /// it, before paying: any cheated ingredient, the no-cost cheat, or a station marked cheated in its ZDO.
    /// </summary>
    public sealed class KitchenCraftContext
    {
        public readonly Player Player;
        public readonly CraftingStation Station;
        public readonly Recipe Recipe;

        /// <summary>The crafted item's prefab name, what Inventory.AddItem(name, ...) takes.</summary>
        public readonly string Prefab;

        public readonly ItemDrop.ItemData.SharedData Shared;
        public readonly int Variant;
        public readonly bool Cheated;
        public readonly int Batches;
        public readonly int ZeroStarsBefore;

        /// <summary>Crafted units the inventory had no room for, held back from the game's add (<see cref="CraftOverflow"/>).</summary>
        public int Overflow;

        public KitchenCraftContext(InventoryGui gui, Player player, CraftingStation station)
        {
            Player = player;
            Station = station;
            Recipe = gui.m_craftRecipe;
            Prefab = Recipe.m_item.gameObject.name;
            Shared = Recipe.m_item.m_itemData.m_shared;
            Variant = gui.m_craftVariant;
            Batches = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
            Cheated = WillBeCheated(player, station, Recipe);
            ZeroStarsBefore = player.GetInventory().CountItems(Shared.m_name, Stars.ToQuality(0));
        }

        /// <summary>Whether the crafted item carries stars, and so rolls them.</summary>
        public bool Grades => Kitchen.IsKitchenName(Shared.m_name);

        public ZNetView StationView => Station == null ? null : Station.m_nview;

        private static bool WillBeCheated(Player player, CraftingStation station, Recipe recipe)
        {
            bool items = player.GetInventory().ItemCheated(recipe.m_resources) || player.NoCostCheat();
            ZNetView view = station == null ? null : station.m_nview;
            bool stationCheated = view != null && view.IsValid() && view.GetZDO().GetBool(ZDOVars.s_cheated);
            return (items || stationCheated) && !PlayerProfile.s_bypassCheatChecks;
        }
    }
}
