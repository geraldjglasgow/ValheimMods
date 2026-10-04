namespace OpenKeep.Tracker
{
    /// <summary>
    /// A craft of a tracked recipe, watched across the game's DoCrafting: the recipe, the quality it makes, how many
    /// crafts (the game's multi-craft amount, which the batch stepper drives) and how many of the item at that quality
    /// the player held before. The craft happened when there are more afterwards; refusals (materials, room, a missing
    /// upgrade item) leave the count as it was.
    /// </summary>
    public sealed class CraftWatch
    {
        private Recipe recipe;
        private int quality;
        private int crafts;
        private int before;

        /// <summary>Null when nothing tracked is being crafted, so most crafts cost nothing.</summary>
        public static CraftWatch Start(InventoryGui gui, Player player)
        {
            Recipe recipe = gui.m_craftRecipe;
            if (recipe == null || player == null || recipe.m_item == null || !TrackerSettings.Enabled.Value)
                return null;
            int quality = gui.m_craftUpgradeItem == null ? 1 : gui.m_craftUpgradeItem.m_quality + 1;
            if (!TrackerList.IsTracked(recipe, quality))
                return null;
            int crafts = gui.m_craftUpgradeItem == null && gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
            return new CraftWatch { recipe = recipe, quality = quality, crafts = crafts, before = Held(player, recipe, quality) };
        }

        public void Finish(Player player)
        {
            if (player != null && Held(player, recipe, quality) > before)
                TrackerList.Crafted(recipe, quality, crafts);
        }

        private static int Held(Player player, Recipe recipe, int quality)
        {
            return player.GetInventory().CountItems(recipe.m_item.m_itemData.m_shared.m_name, quality, false);
        }
    }
}
