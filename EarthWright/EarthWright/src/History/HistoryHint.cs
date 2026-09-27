using System.Linq;
using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// The undo line of the controls hint above the hotbar ("LeftControl + Z undo (3)  LeftControl + Y redo (1)"),
    /// shown while there is something to undo or redo. Rebuilt only when a count or a key changes.
    /// </summary>
    internal static class HistoryHint
    {
        private const string HintKey = "history";

        private static (int Undo, int Redo, KeyboardShortcut UndoKey, KeyboardShortcut RedoKey, string Lang) last;

        public static void Update()
        {
            // Only a machine with a player has a hint to show (never a dedicated server).
            if (Player.m_localPlayer == null)
                return;
            (int Undo, int Redo, KeyboardShortcut UndoKey, KeyboardShortcut RedoKey, string Lang) now =
                (Timeline.Undo.Count, Timeline.Redo.Count, HistorySettings.UndoKey.Value, HistorySettings.RedoKey.Value, Language.CurrentLanguage);
            if (now.Equals(last))
                return;
            last = now;
            if (now.Undo + now.Redo == 0)
            {
                HintText.Set(HintKey, null);
                return;
            }
            HintText.Set(HintKey, Language.Format(HistoryWords.Hint, KeyText(now.UndoKey), now.Undo.ToString(), KeyText(now.RedoKey), now.Redo.ToString()));
        }

        /// <summary>Modifiers first, then the key: "LeftControl + Z".</summary>
        private static string KeyText(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
                return "-";
            return string.Join(" + ", shortcut.Modifiers.Concat(new[] { shortcut.MainKey }).Select(k => k.ToString()));
        }
    }
}
