using System;
using System.Globalization;
using OpenKeep.Core;

namespace OpenKeep.Blueprints
{
    /// <summary>The words of the Blueprints module. Texts with numbers carry $1, $2 placeholders that <see cref="Format"/> fills.</summary>
    public static class BlueprintWords
    {
        public static string Disabled { get; private set; }
        public static string NoBlueprints { get; private set; }
        public static string Unreadable { get; private set; }
        public static string Title { get; private set; }
        public static string Ground { get; private set; }
        public static string GroundWater { get; private set; }
        public static string Earth { get; private set; }
        public static string Materials { get; private set; }
        public static string Free { get; private set; }
        public static string Missing { get; private set; }
        public static string Unloaded { get; private set; }
        public static string InTheWay { get; private set; }
        public static string Unlearned { get; private set; }
        public static string Lacking { get; private set; }
        public static string Aim { get; private set; }
        public static string ClickToPin { get; private set; }
        public static string Pinned { get; private set; }
        public static string Unpinned { get; private set; }
        public static string Outline { get; private set; }
        public static string Progress { get; private set; }
        public static string Done { get; private set; }
        public static string Warded { get; private set; }
        public static string NoBuild { get; private set; }
        public static string Indoors { get; private set; }
        public static string Cleared { get; private set; }
        public static string Limited { get; private set; }
        public static string StoneBack { get; private set; }
        public static string FixName { get; private set; }
        public static string FixDescription { get; private set; }
        public static string FixTitle { get; private set; }
        public static string FixAim { get; private set; }
        public static string FixClick { get; private set; }
        public static string FixPinned { get; private set; }
        public static string FixNothing { get; private set; }
        public static string FixFine { get; private set; }
        public static string FixDone { get; private set; }
        public static string FixUndone { get; private set; }
        public static string FixNoUndo { get; private set; }
        public static string PlannerName { get; private set; }
        public static string PlannerDescription { get; private set; }
        public static string CopyName { get; private set; }
        public static string CopyDescription { get; private set; }
        public static string GhostsName { get; private set; }
        public static string GhostsShownDescription { get; private set; }
        public static string GhostsHiddenDescription { get; private set; }
        public static string GhostsShownBand { get; private set; }
        public static string GhostsHiddenBand { get; private set; }
        public static string GhostsOn { get; private set; }
        public static string GhostsOff { get; private set; }
        public static string Tab { get; private set; }
        public static string TopFolder { get; private set; }
        public static string NewFolderName { get; private set; }
        public static string NewFolderDescription { get; private set; }
        public static string NewFolderTopic { get; private set; }
        public static string FolderMade { get; private set; }
        public static string RenameTopic { get; private set; }
        public static string Renamed { get; private set; }
        public static string BadName { get; private set; }
        public static string Exists { get; private set; }
        public static string IntoItself { get; private set; }
        public static string Moved { get; private set; }
        public static string MoveRefused { get; private set; }
        public static string EntrySize { get; private set; }
        public static string EntryWater { get; private set; }
        public static string EntryKeys { get; private set; }

        public static void Register()
        {
            Disabled = Language.Add("ok_bp_disabled", "Blueprints are switched off on this server");
            NoBlueprints = Language.Add("ok_bp_none", "No blueprints: put blueprint files into BepInEx/config/OpenKeep.Blueprints");
            Unreadable = Language.Add("ok_bp_unreadable", "Cannot read the blueprint: $1");
            Title = Language.Add("ok_bp_title", "Blueprint $1: $2 pieces, $3 x $4 m");
            Ground = Language.Add("ok_bp_ground", "Floor at $1 m");
            GroundWater = Language.Add("ok_bp_groundwater", "Floor at $1 m, set so its water lies below sea level ($2 m)");
            Earth = Language.Add("ok_bp_earth", "Ground: cut $1 m³ (up to $2 m, $3 stone back), fill $4 m³ (up to $5 m, $6 stone)");
            StoneBack = Language.Add("ok_bp_stoneback", "($1 Stone back)");
            Materials = Language.Add("ok_bp_materials", "Materials: $1");
            Free = Language.Add("ok_bp_free", "Materials: free");
            Missing = Language.Add("ok_bp_missing", "Not in this game: $1");
            Unloaded = Language.Add("ok_bp_unloaded", "Part of the site is too far away: come closer");
            InTheWay = Language.Add("ok_bp_intheway", "$1 built pieces stand where the blueprint goes");
            Unlearned = Language.Add("ok_bp_unlearned", "Not learned yet: $1");
            Lacking = Language.Add("ok_bp_lacking", "Missing materials: $1");
            Warded = Language.Add("ok_bp_warded", "A ward you have no access to covers the site");
            NoBuild = Language.Add("ok_bp_nobuild", "Nothing may be built here");
            Indoors = Language.Add("ok_bp_indoors", "Blueprints cannot be placed in here");
            AddPlacing();
        }

        private static void AddPlacing()
        {
            Aim = Language.Add("ok_bp_aim", "Aim at the ground where the blueprint should stand");
            ClickToPin = Language.Add("ok_bp_clicktopin", "Click to pin it here");
            Pinned = Language.Add("ok_bp_pinned", "Pinned: walk around it, click again (or put the hammer away) to place it as a construction site, Backspace to let go");
            Unpinned = Language.Add("ok_bp_unpinned", "Blueprint let go");
            Outline = Language.Add("ok_bp_outline", "Large blueprint: the preview shows its ground floor");
            Progress = Language.Add("ok_bp_progress", "Building $1: $2 of $3 pieces");
            Done = Language.Add("ok_bp_done", "Built $1: $2 pieces");
            Limited = Language.Add("ok_bp_limited", "$1 ground points stop short: the game keeps the ground within 8 m of its natural height");
            Cleared = Language.Add("ok_bp_cleared", "Cleared $1 trees, rocks and shrubs ($2 left alone: warded)");
            AddMenu();
            AddFix();
        }

        private static void AddFix()
        {
            FixName = Language.Add("ok_fix", "Fix ground");
            FixDescription = Language.Add("ok_fix_desc", "Aim at a building: the ground around it is shaped to meet it - cut away where it pokes through a floor, " +
                "filled where a piece hangs above it, with natural slopes all round and dirt under the building. Click to pin the building, click again to fix it. " +
                "Raised ground costs Stone, lowered ground gives Stone back. <color=yellow>Backspace</color> let go  <color=yellow>Alt + Z</color> put the last fix back");
            FixTitle = Language.Add("ok_fix_title", "Fix ground: $1 pieces, $2 touch the ground");
            FixAim = Language.Add("ok_fix_aim", "Aim at a building to fix its ground");
            FixClick = Language.Add("ok_fix_click", "Click to pin this building");
            FixPinned = Language.Add("ok_fix_pinned", "Pinned: click again to fix its ground, Backspace to let go");
            FixNothing = Language.Add("ok_fix_nothing", "No piece of this building touches the ground");
            FixFine = Language.Add("ok_fix_fine", "The ground already fits this building");
            FixDone = Language.Add("ok_fix_done", "Ground fixed under $1 pieces: $2 m3 cut, $3 m3 filled (Alt + Z puts it back)");
            FixUndone = Language.Add("ok_fix_undone", "Ground put back ($1 points)");
            FixNoUndo = Language.Add("ok_fix_noundo", "No ground fix to put back");
            PlannerName = Language.Add("ok_planner", "Site planner");
            CopyName = Language.Add("ok_copy", "Copy building");
            CopyDescription = Language.Add("ok_copy_desc", "Aim at buildings and click to select them (several at once for a whole compound), then Enter saves " +
                "them as a blueprint under a name of your choice, ground included.");
            PlannerDescription = Language.Add("ok_planner_desc", "Aim at a ghost building: click pieces to choose what is built first, Shift + click picks a whole house, " +
                "Enter queues the choice, K opens the queue.");
        }

        private static void AddMenu()
        {
            Tab = Language.Add("ok_bp_tab", "Blueprints");
            EntrySize = Language.Add("ok_bp_entry_size", "$1 pieces, $2 x $3 m");
            EntryWater = Language.Add("ok_bp_entry_water", "Has water: its floor is set from sea level.");
            EntryKeys = Language.Add("ok_bp_entry_keys", "<color=yellow>Click</color> pin, then place the site  <color=yellow>Left / Right</color> turn  " +
                "<color=yellow>Alt + Left / Right</color> turn a little  " +
                "<color=yellow>Home</color> face you  <color=yellow>PageUp / PageDown</color> floor (Alt: 2 m)  <color=yellow>End</color> back on the ground  " +
                "<color=yellow>Backspace</color> let go  <color=yellow>F2</color> rename  In the menu: <color=yellow>right click</color> rename, " +
                "<color=yellow>drag</color> onto a folder on the left or in the bar above to move, <color=yellow>Ctrl / Shift + click</color> or a box to pick several");
            AddFolders();
        }

        private static void AddFolders()
        {
            TopFolder = Language.Add("ok_bp_top", "the top folder");
            NewFolderName = Language.Add("ok_bp_newfolder", "New folder");
            NewFolderDescription = Language.Add("ok_bp_newfolder_desc", "Makes a folder in $1. To put blueprints into a folder, drag them onto it " +
                "on the left or in the bar above (<color=yellow>Ctrl / Shift + click</color> or a box drawn on empty space picks several), or " +
                "rename one to folder/name. A folder moves by renaming it (right click) to parent/name.");
            NewFolderTopic = Language.Add("ok_bp_newfolder_topic", "Name of the new folder");
            FolderMade = Language.Add("ok_bp_foldermade", "Folder $1 made");
            RenameTopic = Language.Add("ok_bp_rename_topic", "New name (folder/name moves it, from the top folder)");
            Renamed = Language.Add("ok_bp_renamed", "$1 is now $2");
            BadName = Language.Add("ok_bp_badname", "'$1' cannot be a file or folder name");
            Exists = Language.Add("ok_bp_exists", "$1 exists already");
            IntoItself = Language.Add("ok_bp_intoitself", "The folder $1 cannot move into itself");
            Moved = Language.Add("ok_bp_moved", "Moved $1 into $2");
            AddGhosts();
            MoveRefused = Language.Add("ok_bp_moverefused", "$1 not moved: $2");
        }

        private static void AddGhosts()
        {
            GhostsName = Language.Add("ok_bp_ghosts", "Construction ghosts");
            GhostsShownDescription = Language.Add("ok_bp_ghosts_shown", "Shown: the see-through unbuilt pieces of construction sites are drawn. " +
                "Click to hide them on this machine (only for you). The posts and building go on as before.");
            GhostsHiddenDescription = Language.Add("ok_bp_ghosts_hidden", "Hidden: the unbuilt pieces of construction sites are not drawn here, " +
                "except while the Site planner is in hand. Click to show them again. The posts and building go on as before.");
            GhostsShownBand = Language.Add("ok_bp_ghosts_band_shown", "Ghosts shown");
            GhostsHiddenBand = Language.Add("ok_bp_ghosts_band_hidden", "Ghosts hidden");
            GhostsOn = Language.Add("ok_bp_ghosts_on", "Construction ghosts shown");
            GhostsOff = Language.Add("ok_bp_ghosts_off", "Construction ghosts hidden (the Site planner still shows them)");
        }

        /// <summary>Translates a $token and puts the values into its $1, $2 ... placeholders.</summary>
        public static string Format(string token, params object[] values)
        {
            string text = Language.Localize(token);
            for (int i = values.Length - 1; i >= 0; i--)
                text = text.Replace("$" + (i + 1), Convert.ToString(values[i], CultureInfo.InvariantCulture));
            return text;
        }

        /// <summary>A length or height as shown to players: one decimal.</summary>
        public static string Metres(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
