using EarthWright.Core;
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
            if (GUILayout.Button(Language.Format(HistoryWords.UndoButton, Timeline.Undo.Count.ToString())))
                UndoActions.Show(UndoActions.Undo);
            if (GUILayout.Button(Language.Format(HistoryWords.RedoButton, Timeline.Redo.Count.ToString())))
                UndoActions.Show(UndoActions.Redo);
            GUILayout.EndHorizontal();
        }
    }
}
