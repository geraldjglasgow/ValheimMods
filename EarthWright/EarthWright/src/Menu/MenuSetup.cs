using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// What the Menu module does when an object database is ready (the main menu's, then the world's, on the server as
    /// on every client, all from the same synced settings). Order 100: find the hoe's and cultivator's tables, record
    /// the game's pieces, make EarthWright's entries, then lay out the menus.
    /// </summary>
    public static class MenuSetup
    {
        public static void Register()
        {
            GameReady.OnObjectDb(100, "EarthWright menu entries", OnEntries);
        }

        private static void OnEntries(ObjectDB db)
        {
            ToolTables.Find(db);
            GamePieces.Capture(ToolTables.Hoe);
            GamePieces.Capture(ToolTables.Cultivator);
            EntryFactory.EnsureAll();
            MenuRefresh.RefreshNow(true);
        }
    }
}
