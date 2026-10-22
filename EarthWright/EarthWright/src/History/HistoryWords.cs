using Translations;

namespace EarthWright.History
{
    /// <summary>The History module's words as $tokens (English in translations/English.yml).</summary>
    public static class HistoryWords
    {
        public static string NothingToUndo { get; } = Lang.Token("ew_history_nothing_undo");
        public static string NothingToRedo { get; } = Lang.Token("ew_history_nothing_redo");
        public static string Undone { get; } = Lang.Token("ew_history_undone");
        public static string Redone { get; } = Lang.Token("ew_history_redone");
        public static string TooFar { get; } = Lang.Token("ew_history_too_far");
        public static string Partly { get; } = Lang.Token("ew_history_partly");
        public static string Unchanged { get; } = Lang.Token("ew_history_unchanged");
        public static string SnapshotSource { get; } = Lang.Token("ew_history_snapshot");
        public static string SnapshotRestored { get; } = Lang.Token("ew_history_snapshot_restored");
        public static string SnapshotSaved { get; } = Lang.Token("ew_history_snapshot_saved");
        public static string SnapshotPartial { get; } = Lang.Token("ew_history_snapshot_partial");
        public static string SnapshotMissing { get; } = Lang.Token("ew_history_snapshot_missing");
        public static string SnapshotSame { get; } = Lang.Token("ew_history_snapshot_same");
        public static string Cleared { get; } = Lang.Token("ew_history_cleared");
        public static string PanelTitle { get; } = Lang.Token("ew_history_panel");
        public static string UndoButton { get; } = Lang.Token("ew_history_undo_button");
        public static string RedoButton { get; } = Lang.Token("ew_history_redo_button");
        public static string ResetSource { get; } = Lang.Token("ew_history_reset");
    }
}
