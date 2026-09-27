using SyncedConfig;

namespace EarthWright.Actions
{
    /// <summary>Entry point of the Actions module: the game's own terrain entries. The placement hook patches itself.</summary>
    public static class ActionsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            VanillaActions.Register();
        }
    }
}
