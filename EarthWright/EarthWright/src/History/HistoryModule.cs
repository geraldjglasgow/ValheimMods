using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.History
{
    /// <summary>
    /// Entry point of the History module (section "7. Undo"): the local undo/redo history of terrain edits, recorded
    /// just before each edit is sent and put back through the normal edit pipeline; and named area snapshots. The
    /// history is personal: it lives on the machine of the player who made the edits.
    /// </summary>
    public static class HistoryModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            HistorySettings.Bind(synced);
            Recorder.Initialize();
            Pruner.Initialize();
            Ticker.OnUpdate("EarthWright press tracker", PressTracker.Sample);
            Ticker.OnUpdate("EarthWright undo keys", UndoActions.OnUpdate);
            HistoryCommands.Register();
            SnapshotCommands.Register();
            HistoryPanel.Register();
        }
    }
}
