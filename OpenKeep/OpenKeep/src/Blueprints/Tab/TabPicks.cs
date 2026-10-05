using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The Blueprints tab's multi-selection, as in a file explorer, of blueprint entries only (never the tools):
    /// Ctrl + click toggles one and makes it the anchor; Shift + click picks every entry from
    /// the anchor to this one in the tab's order (replacing the picks, or adding to them with Ctrl too; without an
    /// anchor it picks just this one); the selection box sets them directly. The picks clear when the menu closes, it
    /// shows another tab or folder, or a plain click acts; entries that leave the folder (moved, deleted) drop out.
    /// </summary>
    public static class TabPicks
    {
        private static readonly List<Piece> picked = new List<Piece>();
        private static Piece anchor;
        private static string folder;

        public static int Count => picked.Count;

        /// <summary>The entry can be picked and dragged: a blueprint.</summary>
        public static bool Pickable(Piece piece) => BlueprintMenu.Owns(piece);

        public static bool Contains(Piece piece) => piece != null && picked.Contains(piece);

        /// <summary>The picks in the tab's order.</summary>
        public static List<Piece> Ordered() => Shown().Where(picked.Contains).ToList();

        /// <summary>A click with Ctrl and/or Shift held on a pickable entry.</summary>
        public static void Click(Piece piece, bool ctrl, bool shift)
        {
            folder = BlueprintLibrary.CurrentFolder;
            if (shift && anchor != null && Range(piece, keep: ctrl))
            {
                TabButtons.Refresh();
                return;
            }
            if (!picked.Remove(piece))
                picked.Add(piece);
            anchor = piece;
            TabButtons.Refresh();
        }

        /// <summary>Every entry from the anchor to <paramref name="to"/>; false when either is not shown any more.</summary>
        private static bool Range(Piece to, bool keep)
        {
            List<Piece> order = Shown();
            int from = order.IndexOf(anchor), until = order.IndexOf(to);
            if (from < 0 || until < 0)
                return false;
            if (!keep)
                picked.Clear();
            for (int i = Mathf.Min(from, until); i <= Mathf.Max(from, until); i++)
            {
                if (!picked.Contains(order[i]))
                    picked.Add(order[i]);
            }
            return true;
        }

        /// <summary>The selection box: the picks it started with (Ctrl held) plus every entry it touches now.</summary>
        public static void Box(List<Piece> kept, List<Piece> touched)
        {
            folder = BlueprintLibrary.CurrentFolder;
            picked.Clear();
            picked.AddRange(kept);
            picked.AddRange(touched.Where(p => !picked.Contains(p)));
            TabButtons.Refresh();
        }

        public static List<Piece> Snapshot() => new List<Piece>(picked);

        /// <summary>A blueprint or folder was renamed or moved: picked blueprints in it follow to their new entries.</summary>
        public static void Moved(string from, string to)
        {
            for (int i = 0; i < picked.Count; i++)
                picked[i] = Follow(picked[i], from, to);
            anchor = anchor != null ? Follow(anchor, from, to) : null;
        }

        private static Piece Follow(Piece piece, string from, string to)
        {
            string path = BlueprintEntries.PathOf(piece);
            string now = BlueprintLibrary.Moved(path, from, to);
            if (now == null || now == path)
                return piece;
            return BlueprintEntries.Blueprint(now).GetComponent<Piece>();
        }

        public static void Clear()
        {
            if (picked.Count == 0 && anchor == null)
                return;
            picked.Clear();
            anchor = null;
            TabButtons.Refresh();
        }

        /// <summary>Per frame: no picks outside the open tab or its folder; entries that left the folder drop out.</summary>
        public static void Tick()
        {
            if (picked.Count == 0 && anchor == null)
                return;
            if (!BlueprintTab.Showing || BlueprintLibrary.CurrentFolder != folder)
            {
                Clear();
                return;
            }
            List<Piece> shown = Shown();
            if (picked.RemoveAll(p => !shown.Contains(p)) > 0)
                TabButtons.Refresh();
            if (anchor != null && !shown.Contains(anchor))
                anchor = null;
        }

        /// <summary>The pickable entries the tab shows, in its order.</summary>
        private static List<Piece> Shown() =>
            BlueprintMenu.View.Where(e => e != null).Select(e => e.GetComponent<Piece>()).Where(Pickable).ToList();
    }
}
