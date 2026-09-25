using System;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The part of the player inventory the bulk actions work on: the rows the game gives the player (its
    /// <c>invrows</c> key: 4, more once rows are bought from the trader), or the first <c>Main Inventory Rows</c>
    /// rows when that is set. Mods that add equipment, food or ammo slots keep them in rows below the game's;
    /// quick stack, store all, dump and sort take nothing from there and the sort puts nothing there. Top up still
    /// fills the stacks there. Every row of a container is main grid.
    /// </summary>
    public static class MainGrid
    {
        private const int GameDefaultRows = 4;

        public static int Rows(Player player, Inventory inventory)
        {
            int height = inventory.GetHeight();
            if (player == null || inventory != player.GetInventory())
                return height;
            int rows = StowSettings.MainInventoryRows.Value;
            return Math.Min(height, rows > 0 ? rows : GameRows(player));
        }

        private static int GameRows(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
                return rows;
            return GameDefaultRows;
        }
    }
}
