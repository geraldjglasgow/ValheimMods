using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.History
{
    /// <summary>
    /// Entry point of the History module (section "7. Undo"): the local undo/redo history of terrain edits, recorded
    /// just before each edit is sent and put back through the normal edit pipeline; named area snapshots; and the
    /// translation file commands. The history is personal: it lives on the machine of the player who made the edits.
    /// </summary>
    public static class HistoryModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            HistorySettings.Bind(synced);
            HistoryWords.Register();
            Recorder.Initialize();
            Pruner.Initialize();
            Ticker.OnUpdate("EarthWright press tracker", PressTracker.Sample);
            Ticker.OnUpdate("EarthWright undo keys", UndoActions.OnUpdate);
            Ticker.OnUpdate("EarthWright undo hint", HistoryHint.Update);
            HistoryCommands.Register();
            SnapshotCommands.Register();
            LanguageCommands.Register();
            HistoryPanel.Register();
            // The game's localization may already exist when the plugin loads; then its words are installed now.
            Language.Reload();
        }
    }
}
