using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// What the Blueprints tab of the game's hammer shows (<see cref="View"/>): the tools first (Fix ground, the Site
    /// planner, Copy building, the Construction ghosts switch), then the blueprints of the folder shown; the folders themselves are on the folder panel
    /// and the breadcrumb (<see cref="Tab.FolderPanel"/>, <see cref="Tab.Breadcrumb"/>).
    /// Every frame it checks the switch, the folder and its files; when one changed the entries go into the hammer's
    /// build table (<see cref="HammerTable"/>), the local player knows them at once without the game's "new piece"
    /// message, and the entry the player chose stays selected (<see cref="BlueprintSelection"/>; a chosen blueprint is
    /// kept in the table though another folder is shown). Off, the table holds none of them.
    /// </summary>
    public static class BlueprintMenu
    {
        private static readonly List<GameObject> view = new List<GameObject>();
        private static (bool On, string Folder, int Version, Player Player, PieceTable Table) shown;

        /// <summary>The tab's entries in order (empty while blueprints are off).</summary>
        public static IReadOnlyList<GameObject> View => view;

        /// <summary>The piece is one of the tab's blueprint entries.</summary>
        public static bool Owns(Piece piece) => Named(piece).StartsWith(BlueprintEntries.BlueprintPrefix);

        /// <summary>The piece is the tab's Fix ground entry.</summary>
        public static bool IsFix(Piece piece) => Named(piece) == BlueprintEntries.FixName;

        /// <summary>The piece is the tab's Site planner entry.</summary>
        public static bool IsPlanner(Piece piece) => Named(piece) == BlueprintEntries.PlannerName;

        /// <summary>The piece is the tab's Copy building entry.</summary>
        public static bool IsCopy(Piece piece) => Named(piece) == BlueprintEntries.CopyName;

        /// <summary>The piece is the tab's Construction ghosts switch (a click flips it; it is never selected).</summary>
        public static bool IsGhosts(Piece piece) => Named(piece) == BlueprintEntries.GhostsName;

        /// <summary>Any of the tab's entries (never a real building piece).</summary>
        public static bool IsOurs(Piece piece) => Owns(piece) || IsFix(piece) || IsPlanner(piece) || IsCopy(piece) || IsGhosts(piece);

        /// <summary>The blueprint path of a blueprint entry ("houses/barn"), or null for any other piece.</summary>
        public static string NameOf(Piece piece) => Owns(piece) ? BlueprintEntries.PathOf(piece) : null;

        /// <summary>Blueprints are on and the local player holds the hammer in build mode with one of the tab's entries selected.</summary>
        public static bool InHand(Player player) =>
            BlueprintSettings.Enabled && player != null && player.InPlaceMode() && IsOurs(player.GetSelectedPiece());

        private static string Named(Piece piece) => piece != null ? Utils.GetPrefabName(piece.gameObject) : "";

        /// <summary>A blueprint or folder was renamed or moved: a selected blueprint inside it stays selected under its new path.</summary>
        public static void Moved(string from, string to)
        {
            BlueprintSelection.Moved(from, to);
            BlueprintSession.FollowMove(from, to);
            Tab.TabPicks.Moved(from, to);
        }

        /// <summary>Per frame: fills the hammer's table again when the switch, the folder, its files, the player or the table changed (never on a dedicated server).</summary>
        public static void Refresh()
        {
            PieceTable table = ZNet.instance != null && ZNet.instance.IsDedicated() ? null : HammerTable.Find();
            if (table == null)
                return;
            Tab.TabMemory.Load();
            Player player = Player.m_localPlayer;
            BlueprintSelection.Track(player, table);
            bool on = BlueprintSettings.Enabled;
            if (on)
                BlueprintLibrary.Names();
            var now = (on, BlueprintLibrary.CurrentFolder, BlueprintLibrary.Version, player, table);
            if (now.Equals(shown) && HammerTable.Intact(table))
                return;
            shown = now;
            Fill(table, player, on);
        }

        private static void Fill(PieceTable table, Player player, bool on)
        {
            GameObject keep = BlueprintSelection.Kept(on);
            view.Clear();
            if (on)
                view.AddRange(Build(BlueprintLibrary.CurrentFolder));
            List<GameObject> entries = new List<GameObject>(view);
            if (keep != null && !entries.Contains(keep))
                entries.Add(keep);
            HammerTable.Put(table, entries);
            if (player != null)
                Learn(player, entries);
            BlueprintSelection.Filled();
            BlueprintSelection.Track(player, table);
            BlueprintTab.Redraw();
        }

        /// <summary>The entries of a folder, in the tab's order.</summary>
        private static IEnumerable<GameObject> Build(string folder)
        {
            List<GameObject> made = new List<GameObject>
            {
                BlueprintEntries.Fix(), BlueprintEntries.Planner(), BlueprintEntries.Copy(), BlueprintEntries.Ghosts(),
            };
            made.AddRange(BlueprintLibrary.Names(folder).Select(BlueprintEntries.Blueprint));
            return made;
        }

        /// <summary>The entries are known to the player (no unlock message), then the player's build lists are made again.</summary>
        private static void Learn(Player player, List<GameObject> entries)
        {
            foreach (GameObject entry in entries)
                player.m_knownRecipes.Add(entry.GetComponent<Piece>().m_name);
            player.UpdateAvailablePiecesList();
        }

        /// <summary>Player.AddKnownPiece prefix: an entry becomes known silently. False when it handled the piece.</summary>
        public static bool LearnQuietly(Player player, Piece piece)
        {
            if (!IsOurs(piece))
                return true;
            player.m_knownRecipes.Add(piece.m_name);
            return false;
        }
    }
}
