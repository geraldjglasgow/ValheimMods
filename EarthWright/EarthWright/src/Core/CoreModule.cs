using SyncedConfig;

namespace EarthWright.Core
{
    /// <summary>Entry point of the Core module: section 0, the root console command and its general subcommands.</summary>
    public static class CoreModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            GeneralSettings.Bind(synced);
            CoreCommands.Register();
            Keys.Register();
            OwnedPick.Initialize();
        }
    }
}
