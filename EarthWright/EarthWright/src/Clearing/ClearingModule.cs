using EarthWright.Actions;
using SyncedConfig;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Entry point of the Clearing module (section "5. Reset and Clearing"): the reset keys and 'ew reset', the Clear
    /// and Groundbreaker entries (special actions "clear" and "groundbreaker"; the Menu module makes their pieces),
    /// survival clearing, the clearing commands and the admin 'ew terrain' and 'ew pieces'.
    /// </summary>
    public static class ClearingModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            ClearingSettings.Bind(synced);
            ClearingWords.Register();
            ResetKeys.Initialize();
            SurvivalFollowUp.Initialize();
            ClearQueue.Initialize();
            ClearingCommands.Register();
            TerrainCommand.Register();
            SpecialActions.Register("clear", new ClearAction());
            SpecialActions.Register("groundbreaker", new GroundbreakerAction());
        }
    }
}
