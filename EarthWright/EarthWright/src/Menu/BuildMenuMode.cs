using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// The game gives the hoe and the cultivator a simplified build menu (<c>PieceTable.m_hideAdvancedMenu</c>): a plain
    /// list without the tag column, and the search field lives in that column (BuildUIV2 ... TagList/LayoutGroup/SearchBar),
    /// so there is no search, no recent pieces and no favourites. With "Full Build Menu" on (each player's own choice)
    /// the terrain tools get the full layout the hammer has; their original mode comes back when the setting is off or
    /// EarthWright is not active for this player. The flag is read only by the build menu, so nothing else changes.
    /// </summary>
    public static class BuildMenuMode
    {
        private static readonly Dictionary<PieceTable, bool> originals = new Dictionary<PieceTable, bool>();

        public static void Apply()
        {
            bool full = GeneralSettings.Active && MenuSettings.FullBuildMenu.Value;
            Set(ToolTables.Hoe, full);
            Set(ToolTables.Cultivator, full);
        }

        /// <summary>Sets the table's mode, remembering its original simplified flag the first time.</summary>
        private static void Set(PieceTable table, bool full)
        {
            if (table == null)
                return;
            if (!originals.TryGetValue(table, out bool simplified))
            {
                simplified = table.m_hideAdvancedMenu;
                originals[table] = simplified;
            }
            table.m_hideAdvancedMenu = !full && simplified;
        }
    }
}
