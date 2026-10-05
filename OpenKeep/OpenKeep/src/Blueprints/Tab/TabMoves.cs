using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// Dropping dragged entries of the Blueprints tab into a folder: each blueprint is moved there through
    /// <see cref="BlueprintFiles.Rename"/> (a path from the top folder, so only its place changes), one at a time, so a
    /// refused one (its name taken there, a folder into itself) says why at the top left and the rest still move. A
    /// selected or pinned blueprint follows its file; the picks clear and the tab is filled again once at the end.
    /// </summary>
    public static class TabMoves
    {
        public static void Into(List<Piece> entries, string folder)
        {
            if (folder == null)
                return;
            int moved = 0;
            foreach (Piece entry in entries)
            {
                if (Move(entry, folder))
                    moved++;
            }
            TabPicks.Clear();
            BlueprintMenu.Refresh();
            if (moved > 0)
                Messages.Center(BlueprintWords.Format(BlueprintWords.Moved, moved, Shown(folder)));
        }

        /// <summary>Moves one entry into the folder; a refusal is shown at the top left. True when it moved.</summary>
        private static bool Move(Piece entry, string folder)
        {
            string from = BlueprintMenu.NameOf(entry);
            if (from == null)
                return false;
            string typed = "/" + BlueprintLibrary.Join(folder, BlueprintLibrary.Leaf(from));
            if (BlueprintFiles.Rename(from, false, typed, out string to, out string error))
            {
                BlueprintMenu.Moved(from, to);
                return true;
            }
            if (error != null)
                Messages.TopLeft(BlueprintWords.Format(BlueprintWords.MoveRefused, BlueprintLibrary.Leaf(from), error));
            return false;
        }

        private static string Shown(string folder) => BlueprintEntries.Shown(folder);
    }
}
