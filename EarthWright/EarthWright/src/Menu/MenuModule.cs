using EarthWright.Actions;
using SyncedConfig;

namespace EarthWright.Menu
{
    /// <summary>
    /// Entry point of the Menu module: EarthWright's own build-menu entries (cloned game pieces with their own icons),
    /// the entry toggles and the game entries' behaviour settings (section 12), descriptions that list the configured
    /// keys, the full build menu (search, favourites) for the terrain tools, and custom entries from
    /// EarthWright.Entries.yml that run console commands.
    /// </summary>
    public static class MenuModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            MenuSettings.Bind(synced);
            MenuWords.Register();
            foreach (EntryDef def in EntryDefs.All)
                ActionCatalog.Register(def.Action);
            CustomEntries.Register(synced);
            SpecialActions.Register("custom", new CustomCommand());
            GameEntryBehaviour.Apply();
            TerraformLimits.Register();
            MenuSetup.Register();
            MenuRefresh.Initialize(synced);
            CustomRepeat.Initialize();
            CustomStartValues.Initialize();
        }
    }
}
