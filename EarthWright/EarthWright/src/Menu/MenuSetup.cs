using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// What the Menu module does when an object database is ready (the main menu's, then the world's, on the server as
    /// on every client, all from the same synced settings). Order 100: find the hoe's and cultivator's tables, record
    /// the game's pieces, make EarthWright's entries, then lay out the menus. Order 300, once other modules' tools exist
    /// (the shovel at 200): add the custom shovel entries to those tools' tables.
    /// </summary>
    public static class MenuSetup
    {
        public static void Register()
        {
            GameReady.OnObjectDb(100, "EarthWright menu entries", OnEntries);
            GameReady.OnObjectDb(300, "EarthWright entries on other tools", OnOtherTools);
        }

        private static void OnEntries(ObjectDB db)
        {
            ToolTables.Find(db);
            GamePieces.Capture(ToolTables.Hoe);
            GamePieces.Capture(ToolTables.Cultivator);
            EntryFactory.EnsureAll();
            MenuRefresh.RefreshNow(true);
        }

        private static void OnOtherTools(ObjectDB db)
        {
            ToolTables.FindOthers(db);
            ToolTables.ApplyAll();
            BuildMenuMode.Apply();
        }
    }
}
