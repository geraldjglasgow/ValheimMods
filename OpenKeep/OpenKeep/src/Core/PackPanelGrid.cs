using System.Collections.Generic;
using System.Globalization;

namespace OpenKeep.Core
{
    /// <summary>
    /// The local player's main grid as PackPanel publishes it: the character's custom data key <c>PackPanel.mainGrid</c>
    /// holds "width|rows|blocked". The main grid is the first <c>rows</c> rows (a worn backpack's included); the last
    /// <c>blocked</c> cells of its bottom row hold nothing; every row below is PackPanel's slots (worn gear, utilities,
    /// food, mead, ammo, the purse, the key ring), which quick stack, store all, dump, sort and a shared chest's stack
    /// all leave alone. Trusted only while PackPanel lays out the inventory: the key is saved with the character and
    /// outlives the mod.
    /// </summary>
    public static class PackPanelGrid
    {
        public const string Key = "PackPanel.mainGrid";

        private static string parsedFrom;
        private static int width, rows, blocked;

        /// <summary>The player's main grid while PackPanel lays it out; false otherwise.</summary>
        public static bool TryRead(Player player, out int mainWidth, out int mainRows, out int blockedCells)
        {
            mainWidth = mainRows = blockedCells = 0;
            if (player == null || !PackPanelLink.LaysOutInventory || !player.m_customData.TryGetValue(Key, out string value))
                return false;
            if (!ReferenceEquals(value, parsedFrom) && !Parse(value))
                return false;
            mainWidth = width;
            mainRows = rows;
            blockedCells = blocked;
            return true;
        }

        /// <summary>Whether an item of the local player's own inventory lies below the main grid, in a PackPanel slot.</summary>
        public static bool InSlot(Player player, Inventory inventory, ItemDrop.ItemData item) =>
            item != null && player != null && inventory == player.GetInventory()
            && TryRead(player, out _, out int mainRows, out _) && item.m_gridPos.y >= mainRows;

        /// <summary>The cells of the bottom main row that hold nothing (a backpack's partly used last row); else none.</summary>
        public static IEnumerable<Vector2i> BlockedCells(Player player, Inventory inventory)
        {
            if (player == null || inventory != player.GetInventory() || !TryRead(player, out int w, out int r, out int b))
                yield break;
            for (int x = w - b; x < w; x++)
                yield return new Vector2i(x, r - 1);
        }

        private static bool Parse(string value)
        {
            string[] parts = value.Split('|');
            if (parts.Length != 3
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
                return false;
            width = w;
            rows = r;
            blocked = b;
            parsedFrom = value;
            return true;
        }
    }
}
