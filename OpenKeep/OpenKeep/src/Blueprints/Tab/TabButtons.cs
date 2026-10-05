using OpenKeep.Core;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// What OpenKeep adds to the build menu's piece buttons, set again every time the menu sets a button up for a piece
    /// (a BuildUiPieceButton.Setup postfix), since the menu reuses its buttons for whatever it shows next: the name band
    /// on blueprint entries and the ghosts switch's state (<see cref="TabLabel"/>), the pick mark (<see cref="TabMark"/>),
    /// and the drag handle (<see cref="TabDrag"/>), switched on only for blueprints so every other button
    /// leaves its drags to the list's scrolling as before. Buttons that never showed an entry of the tab get nothing.
    /// </summary>
    public static class TabButtons
    {
        public static void Decorate(BuildUiPieceButton button)
        {
            Piece piece = button.Piece;
            TabLabel.Show(button, LabelOf(piece));
            TabDrag drag = button.GetComponent<TabDrag>();
            bool draggable = TabPicks.Pickable(piece);
            if (drag == null && draggable)
                drag = button.gameObject.AddComponent<TabDrag>();
            if (drag != null)
                drag.enabled = draggable;
            TabMark.Set(button, StateOf(piece));
        }

        /// <summary>The marks of every button the open menu shows, after the picks or the drop target changed.</summary>
        public static void Refresh()
        {
            BuildUi menu = BlueprintTab.Menu;
            if (menu == null)
                return;
            foreach (BuildUiPieceButton button in menu.m_pieceButtons)
            {
                if (button != null)
                    TabMark.Set(button, StateOf(button.Piece));
            }
        }

        /// <summary>The name a button shows: blueprints their name, the ghosts switch its state, the other tools none.</summary>
        private static string LabelOf(Piece piece)
        {
            if (BlueprintMenu.IsGhosts(piece))
                return Language.Localize(GhostSwitch.Shown ? BlueprintWords.GhostsShownBand : BlueprintWords.GhostsHiddenBand);
            return TabPicks.Pickable(piece) ? Language.Localize(piece.m_name) : null;
        }

        private static TabMark.State StateOf(Piece piece) => TabPicks.Contains(piece) ? TabMark.State.Picked : TabMark.State.None;
    }
}
