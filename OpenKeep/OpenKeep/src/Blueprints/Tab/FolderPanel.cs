using System;
using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The folder panel: the build menu's own left column (where the other tabs list their tags, under its search field,
    /// scrolled by the wheel) lists folders while the Blueprints tab is shown. First the folder above, with the up icon
    /// (a click goes up; at the top folder this row is "Blueprints" itself and does nothing) with the New folder button at
    /// its right end, then every folder in it (the folder shown and its neighbours), the shown one highlighted, and under
    /// it, indented, its own subfolders.
    /// The rows are the game's own tag buttons (<see cref="FolderRows"/>), kept after its own and hidden on every other
    /// tab, where the game's "All" row comes back. Rebuilt when the folder or the files change.
    /// </summary>
    public static class FolderPanel
    {
        /// <summary>One row to show: its folder, text, depth in the little tree and what a click and a right click do.</summary>
        public struct Line
        {
            public string Folder;
            public string Text;
            public int Depth;
            public bool Up;
            public bool Current;
            public bool Opens;
        }

        private static BuildUi menu;
        private static string built;
        private static bool allHidden;

        /// <summary>BuildUi.Awake: the panel belongs to this menu (its rows are made when first shown).</summary>
        public static void Install(BuildUi ui)
        {
            menu = ui;
            built = null;
            allHidden = false;
            FolderRows.Reset(ui);
        }

        /// <summary>Per frame and whenever the game redraws its tag column: the rows exactly while the tab is shown.</summary>
        public static void Sync()
        {
            if (menu == null)
                return;
            bool showing = BlueprintTab.Showing;
            ShowGameAll(!showing);
            if (!showing)
            {
                if (built != null)
                    FolderRows.HideFrom(0);
                built = null;
                return;
            }
            string key = BlueprintLibrary.CurrentFolder + "|" + BlueprintLibrary.Version;
            if (key == built)
                return;
            built = key;
            FolderRows.Show(Lines(BlueprintLibrary.CurrentFolder));
            NewFolderBadge.Describe(BlueprintLibrary.CurrentFolder);
        }

        /// <summary>The game's "All" row is hidden on this tab (it means nothing here) and given back on the others.</summary>
        private static void ShowGameAll(bool show)
        {
            BuildUiTagButton all = menu.m_showAllTagsButton;
            if (all == null)
                return;
            if (!show && all.gameObject.activeSelf)
            {
                all.gameObject.SetActive(false);
                allHidden = true;
            }
            else if (show && allHidden)
            {
                all.gameObject.SetActive(true);
                allHidden = false;
            }
        }

        /// <summary>The rows for the folder shown: the folder above (or the top), its folders, and the shown one's own.</summary>
        private static List<Line> Lines(string current)
        {
            List<Line> lines = new List<Line>();
            if (current.Length == 0)
            {
                lines.Add(new Line { Folder = "", Text = NameOf(""), Up = true, Current = true });
                AddChildren(lines, "", 1);
                return lines;
            }
            string parent = BlueprintLibrary.Parent(current);
            lines.Add(new Line { Folder = parent, Text = NameOf(parent), Up = true, Opens = true });
            foreach (string sibling in BlueprintLibrary.Folders(parent))
            {
                bool here = string.Equals(sibling, current, StringComparison.OrdinalIgnoreCase);
                lines.Add(new Line { Folder = sibling, Text = NameOf(sibling), Depth = 1, Current = here, Opens = !here });
                if (here)
                    AddChildren(lines, sibling, 2);
            }
            return lines;
        }

        private static void AddChildren(List<Line> lines, string folder, int depth)
        {
            foreach (string child in BlueprintLibrary.Folders(folder))
                lines.Add(new Line { Folder = child, Text = NameOf(child), Depth = depth, Opens = true });
        }

        /// <summary>A folder as the panel and the breadcrumb show it: "Blueprints" for the top, else its name made readable.</summary>
        public static string NameOf(string folder) =>
            string.IsNullOrEmpty(folder) ? Language.Localize(BlueprintWords.Tab) : BlueprintEntries.Title(BlueprintLibrary.Leaf(folder));
    }
}
