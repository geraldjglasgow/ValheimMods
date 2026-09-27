using System.Collections.Generic;
using EarthWright.Core;

namespace EarthWright.History
{
    /// <summary>
    /// Undo and redo: the newest step of one list is put back on the ground and the values it replaced go on the other
    /// list. A step that cannot be put back (its ground is not loaded, or a guard refuses) stays where it was. Costs are
    /// not refunded. The keys work while a terrain tool is out; the undo key first lets the ramp and road tools remove
    /// their last point (<see cref="UndoHooks"/>).
    /// </summary>
    internal static class UndoActions
    {
        public delegate bool StepAction(out string message);

        public static bool Undo(out string message) =>
            Move(Timeline.Undo, Timeline.Redo, "undo", HistoryWords.NothingToUndo, HistoryWords.Undone, out message);

        public static bool Redo(out string message) =>
            Move(Timeline.Redo, Timeline.Undo, "redo", HistoryWords.NothingToRedo, HistoryWords.Redone, out message);

        /// <summary>Per frame: the undo and redo keys.</summary>
        public static void OnUpdate()
        {
            if (!LocalTool.InTerrainTool || !GeneralSettings.Active)
                return;
            if (Keys.Pressed(HistorySettings.UndoKey))
            {
                if (!UndoHooks.TryFirst())
                    Show(Undo);
            }
            else if (Keys.Pressed(HistorySettings.RedoKey))
            {
                Show(Redo);
            }
        }

        /// <summary>Runs an action and shows its message in the middle of the screen.</summary>
        public static bool Show(StepAction action)
        {
            bool done = action(out string message);
            Messages.Center(message);
            return done;
        }

        private static bool Move(List<Step> from, List<Step> to, string source, string nothing, string done, out string message)
        {
            Step step = Timeline.Pop(from);
            if (step == null)
            {
                message = nothing;
                return false;
            }
            RestoreOutcome outcome = Restorer.Apply(step, source);
            if (outcome.TooFar || outcome.Refusal != null)
            {
                Timeline.Push(from, step);
                message = outcome.TooFar ? HistoryWords.TooFar : outcome.Refusal;
                return false;
            }
            Timeline.Push(to, outcome.Inverse);
            Timeline.Push(from, Restorer.Remainder(step, outcome));
            message = Result(step, outcome, done);
            return outcome.Sent > 0;
        }

        private static string Result(Step step, RestoreOutcome outcome, string done)
        {
            if (outcome.Left.Count > 0)
                return HistoryWords.Partly;
            if (outcome.Sent == 0)
                return Language.Format(HistoryWords.Unchanged, step.Describe());
            return Language.Format(done, step.Describe());
        }
    }
}
