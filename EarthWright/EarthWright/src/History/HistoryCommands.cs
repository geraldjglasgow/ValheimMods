using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>The console subcommands <c>ew undo [steps]</c>, <c>ew redo [steps]</c> and <c>ew history [clear]</c>.</summary>
    internal static class HistoryCommands
    {
        public static void Register()
        {
            Command.Add("undo", "undo [steps]       takes back your last terrain changes, newest first (the undo key)", args => Repeat(args, UndoActions.Undo));
            Command.Add("redo", "redo [steps]       brings back the changes you undid (the redo key)", args => Repeat(args, UndoActions.Redo));
            Command.Add("history", "history [clear]    lists your undo and redo steps, or forgets them", History);
        }

        /// <summary>Runs undo or redo the given number of times (default once), stopping at the first that cannot be done.</summary>
        private static void Repeat(Terminal.ConsoleEventArgs args, UndoActions.StepAction action)
        {
            int steps = Mathf.Clamp(args.TryParameterInt(2, 1), 1, 50);
            for (int i = 0; i < steps; i++)
            {
                bool done = action(out string message);
                args.Context.AddString(Language.Localize(message));
                if (i == steps - 1 || !done)
                    Messages.Center(message);
                if (!done)
                    return;
            }
        }

        private static void History(Terminal.ConsoleEventArgs args)
        {
            if (args.Length > 2 && args[2].ToLowerInvariant() == "clear")
            {
                Timeline.Clear();
                args.Context.AddString(Language.Localize(HistoryWords.Cleared));
                return;
            }
            args.Context.AddString($"EarthWright undo history: {Timeline.Undo.Count} to undo, {Timeline.Redo.Count} to redo (at most {HistorySettings.Size.Value} each).");
            List(args, Timeline.Undo, "undo");
            List(args, Timeline.Redo, "redo");
        }

        /// <summary>One line per step, newest first: the one the next undo (or redo) takes is number 1.</summary>
        private static void List(Terminal.ConsoleEventArgs args, List<Step> steps, string label)
        {
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                Step step = steps[i];
                int ago = Mathf.RoundToInt(Time.time - step.LastSend);
                args.Context.AddString($"  {label} {steps.Count - i}: {step.Describe()} - {step.Edits} edits, {step.Comps.Count} areas, {step.Points} points, {ago} s ago");
            }
        }
    }
}
