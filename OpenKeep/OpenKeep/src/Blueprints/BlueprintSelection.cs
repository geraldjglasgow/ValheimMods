using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Keeps the hammer's selection on the entry of the Blueprints tab the player chose. The game remembers a selection
    /// as a place in a category's list, and a fill of the tab (another folder, a new or renamed file) moves the places in
    /// the tab's category, so after a fill the selection is put back on the chosen entry as soon as the hammer is out
    /// (at once, or when it is taken out again). A chosen blueprint stays in the table while another folder is shown,
    /// and follows its file when that is renamed or moved.
    /// </summary>
    public static class BlueprintSelection
    {
        private static Player tracked;
        private static GameObject chosen;
        private static bool verify;

        /// <summary>A blueprint path the chosen entry moved to (a rename), taken at the next fill.</summary>
        private static string follow;

        /// <summary>The chosen blueprint's entry (under its new path after a rename) for the table to keep, or null.</summary>
        public static GameObject Kept(bool on)
        {
            string path = follow ?? BlueprintMenu.NameOf(Chosen);
            follow = null;
            if (!on || path == null)
                return null;
            chosen = BlueprintEntries.Blueprint(path);
            return chosen;
        }

        /// <summary>A blueprint or folder was renamed or moved: a chosen blueprint inside it is chosen again under its new path.</summary>
        public static void Moved(string from, string to)
        {
            string path = BlueprintMenu.NameOf(Chosen);
            string now = BlueprintLibrary.Moved(path, from, to);
            if (now != path)
                follow = now;
        }

        /// <summary>A fill happened: the selection is checked against the chosen entry at the next look.</summary>
        public static void Filled() => verify = true;

        /// <summary>
        /// With the hammer out: after a fill, puts a selection that moved back on the chosen entry; otherwise remembers
        /// which entry of the tab is selected (none for a game piece). A new local player starts with nothing chosen.
        /// </summary>
        public static void Track(Player player, PieceTable table)
        {
            if (!ReferenceEquals(player, tracked))
            {
                tracked = player;
                chosen = null;
                verify = false;
            }
            if (player == null || player.GetBuildTool() != table)
                return;
            Piece selected = player.GetSelectedPiece();
            GameObject now = selected != null ? selected.gameObject : null;
            if (verify && chosen != null && now != chosen && table.m_pieces.Contains(chosen))
                player.SetSelectedPiece(chosen.GetComponent<Piece>());
            else
                chosen = BlueprintMenu.IsOurs(selected) ? now : null;
            verify = false;
        }

        private static Piece Chosen => chosen != null ? chosen.GetComponent<Piece>() : null;
    }
}
