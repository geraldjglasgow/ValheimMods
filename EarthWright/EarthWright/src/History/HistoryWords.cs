using EarthWright.Core;

namespace EarthWright.History
{
    /// <summary>The History module's words ($tokens), registered in English; translation files may replace them.</summary>
    public static class HistoryWords
    {
        public static string NothingToUndo { get; private set; }
        public static string NothingToRedo { get; private set; }
        public static string Undone { get; private set; }
        public static string Redone { get; private set; }
        public static string TooFar { get; private set; }
        public static string Partly { get; private set; }
        public static string Unchanged { get; private set; }
        public static string SnapshotSource { get; private set; }
        public static string SnapshotRestored { get; private set; }
        public static string SnapshotSaved { get; private set; }
        public static string SnapshotPartial { get; private set; }
        public static string SnapshotMissing { get; private set; }
        public static string SnapshotSame { get; private set; }
        public static string Cleared { get; private set; }
        public static string PanelTitle { get; private set; }
        public static string UndoButton { get; private set; }
        public static string RedoButton { get; private set; }
        public static string Hint { get; private set; }
        public static string ResetSource { get; private set; }

        public static void Register()
        {
            NothingToUndo = Language.Add("ew_history_nothing_undo", "Nothing to undo");
            NothingToRedo = Language.Add("ew_history_nothing_redo", "Nothing to redo");
            Undone = Language.Add("ew_history_undone", "Undone: $1");
            Redone = Language.Add("ew_history_redone", "Redone: $1");
            TooFar = Language.Add("ew_history_too_far", "That ground is too far away: go closer to change it back");
            Partly = Language.Add("ew_history_partly", "Only part of it could be changed back; press again for the rest");
            Unchanged = Language.Add("ew_history_unchanged", "$1: the ground already looks like that");
            SnapshotSource = Language.Add("ew_history_snapshot", "snapshot $1");
            SnapshotRestored = Language.Add("ew_history_snapshot_restored", "Snapshot restored: $1");
            SnapshotSaved = Language.Add("ew_history_snapshot_saved", "Snapshot $1 saved: $2 points");
            SnapshotPartial = Language.Add("ew_history_snapshot_partial", "Part of the area is not loaded and was left out");
            SnapshotMissing = Language.Add("ew_history_snapshot_missing", "There is no snapshot named $1");
            SnapshotSame = Language.Add("ew_history_snapshot_same", "The ground already matches snapshot $1");
            Cleared = Language.Add("ew_history_cleared", "Undo history cleared");
            PanelTitle = Language.Add("ew_history_panel", "Undo");
            UndoButton = Language.Add("ew_history_undo_button", "Undo ($1)");
            RedoButton = Language.Add("ew_history_redo_button", "Redo ($1)");
            Hint = Language.Add("ew_history_hint", "$1 undo ($2)   $3 redo ($4)");
            ResetSource = Language.Add("ew_history_reset", "Reset");
        }
    }
}
