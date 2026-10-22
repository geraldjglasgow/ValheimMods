using EarthWright.Core;
using Translations;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>The "Undo" section of the EarthWright panel: undo and redo buttons with the number of steps each can take.</summary>
    internal static class HistoryPanel
    {
        public const int Order = 700;

        public static void Register()
        {
            PanelSections.Add(Order, HistoryWords.PanelTitle, Draw);
        }

        private static void Draw()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Lang.Format(HistoryWords.UndoButton, Timeline.Undo.Count)))
                UndoActions.Show(UndoActions.Undo);
            if (GUILayout.Button(Lang.Format(HistoryWords.RedoButton, Timeline.Redo.Count)))
                UndoActions.Show(UndoActions.Redo);
            GUILayout.EndHorizontal();
        }
    }
}
