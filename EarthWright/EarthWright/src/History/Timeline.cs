using System.Collections.Generic;

namespace EarthWright.History
{
    /// <summary>
    /// The local player's undo and redo lists, newest last, each at most "History Size" long (the oldest step drops
    /// out). A new change clears the redo list; undo and redo move steps between the two lists.
    /// </summary>
    internal static class Timeline
    {
        public static readonly List<Step> Undo = new List<Step>();
        public static readonly List<Step> Redo = new List<Step>();

        /// <summary>The newest step while new edits may still join it (same held press, or within the group window).</summary>
        public static Step Open;

        /// <summary>A new change: goes on the undo list, and what was undone before can no longer be redone.</summary>
        public static void Record(Step step)
        {
            Redo.Clear();
            Push(Undo, step);
        }

        /// <summary>Puts a step on a list without touching the other (undo and redo results, or a step handed back).</summary>
        public static void Push(List<Step> list, Step step)
        {
            if (step == null || step.IsEmpty)
                return;
            list.Add(step);
            Trim(list);
        }

        /// <summary>Takes the newest step off a list, or null when it is empty. Closes the open step.</summary>
        public static Step Pop(List<Step> list)
        {
            Open = null;
            Trim(list);
            if (list.Count == 0)
                return null;
            Step step = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return step;
        }

        /// <summary>Removes undo steps that were pruned down to nothing (their edits changed no ground).</summary>
        public static void DropEmpty()
        {
            Undo.RemoveAll(step => step.IsEmpty);
            if (Open != null && !Undo.Contains(Open))
                Open = null;
        }

        public static void Clear()
        {
            Undo.Clear();
            Redo.Clear();
            Open = null;
        }

        private static void Trim(List<Step> list)
        {
            int max = HistorySettings.Size != null ? HistorySettings.Size.Value : 15;
            if (list.Count > max)
                list.RemoveRange(0, list.Count - max);
        }
    }
}
