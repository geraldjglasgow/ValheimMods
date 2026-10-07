namespace Hearthhold
{
    /// <summary>
    /// One craft at a kitchen crafting station, captured before InventoryGui.DoCrafting runs: what is made, by whom, at
    /// what levels (the Cooking level, and the Fishing level for the Angler floor, read before the craft raises a
    /// skill), the crafter's count of the item without stars, and the variant and cheated flag the game gives the
    /// crafted items. The cheated flag is worked out as DoCrafting does it, before paying: any cheated ingredient, the
    /// no-cost cheat, or a station marked cheated in its ZDO.
    /// </summary>
    public sealed class KitchenCraftContext
    {
        public readonly Player Player;
        public readonly ItemDrop Product;

        /// <summary>The crafted item's prefab name, what Inventory.AddItem(name, ...) takes.</summary>
        public readonly string Prefab;

        public readonly ItemDrop.ItemData.SharedData Shared;
        public readonly int Variant;
        public readonly bool Cheated;
        public readonly int PlainBefore;
        public readonly float CookLevel;
        public readonly float FishingLevel;

        public KitchenCraftContext(InventoryGui gui, Player player, CraftingStation station)
        {
            Player = player;
            Product = gui.m_craftRecipe.m_item;
            Prefab = Product.gameObject.name;
            Shared = Product.m_itemData.m_shared;
            Variant = gui.m_craftVariant;
            Cheated = WillBeCheated(player, station, gui.m_craftRecipe);
            PlainBefore = PlainCount(player.GetInventory(), Shared.m_name);
            CookLevel = GrindstoneLink.LocalLevel(StarSources.Skill(StarSource.Dish));
            FishingLevel = GrindstoneLink.LocalLevel(StarSources.Skill(StarSource.Fillet));
        }

        /// <summary>Whether the crafted item carries stars, and so rolls them.</summary>
        public bool Grades => Stars.IsStarItem(Product.m_itemData);

        /// <summary>Units of the item at quality 1 (no stars), at any world level.</summary>
        public static int PlainCount(Inventory inventory, string name)
        {
            int count = 0;
            foreach (ItemDrop.ItemData item in inventory.m_inventory)
            {
                if (item.m_shared.m_name == name && item.m_quality == Stars.ToQuality(0))
                    count += item.m_stack;
            }
            return count;
        }

        private static bool WillBeCheated(Player player, CraftingStation station, Recipe recipe)
        {
            bool items = player.GetInventory().ItemCheated(recipe.m_resources) || player.NoCostCheat();
            ZNetView view = station == null ? null : station.m_nview;
            bool stationCheated = view != null && view.IsValid() && view.GetZDO().GetBool(ZDOVars.s_cheated);
            return (items || stationCheated) && !PlayerProfile.s_bypassCheatChecks;
        }
    }
}
