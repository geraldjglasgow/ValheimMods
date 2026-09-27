using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// The game gives the hoe and the cultivator a simplified build menu (<c>PieceTable.m_hideAdvancedMenu</c>): a plain
    /// list without the tag column, and the search field lives in that column (BuildUIV2 ... TagList/LayoutGroup/SearchBar),
    /// so there is no search, no recent pieces and no favourites. With "Full Build Menu" on (each player's own choice)
    /// the terrain tools get the full layout the hammer has; their original mode comes back when the setting is off or
    /// EarthWright is not active for this player. Other terrain tools (the shovel, whose table copies the hoe's settings
    /// when it is made, possibly after this changed them) follow the hoe's original mode. The flag is read only by the
    /// build menu, so nothing else changes.
    /// </summary>
    public static class BuildMenuMode
    {
        private static readonly Dictionary<PieceTable, bool> originals = new Dictionary<PieceTable, bool>();

        public static void Apply()
        {
            bool full = GeneralSettings.Active && MenuSettings.FullBuildMenu.Value;
            bool hoeOriginal = Set(ToolTables.Hoe, full, null);
            Set(ToolTables.Cultivator, full, null);
            foreach (PieceTable table in ToolTables.Others)
                Set(table, full, hoeOriginal);
        }

        /// <summary>Sets the table's mode and returns its original simplified flag (<paramref name="original"/> when given).</summary>
        private static bool Set(PieceTable table, bool full, bool? original)
        {
            if (table == null)
                return true;
            if (!originals.TryGetValue(table, out bool simplified))
            {
                simplified = original ?? table.m_hideAdvancedMenu;
                originals[table] = simplified;
            }
            table.m_hideAdvancedMenu = !full && simplified;
            return simplified;
        }
    }
}
