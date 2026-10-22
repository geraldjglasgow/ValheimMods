using System.Globalization;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The keys of the Blueprints module's words. Their English is in <c>translations/English.blueprints.yml</c>; the
    /// Translations library gives them in the player's language (<c>Lang.Get</c>; <c>Lang.Format</c> fills the $1, $2
    /// placeholders; <c>Lang.Token</c> for a field the game translates itself).
    /// </summary>
    public static class BlueprintWords
    {
        public const string Disabled = "ok_bp_disabled";
        public const string NoBlueprints = "ok_bp_none";
        public const string Unreadable = "ok_bp_unreadable";
        public const string Title = "ok_bp_title";
        public const string Ground = "ok_bp_ground";
        public const string GroundWater = "ok_bp_groundwater";
        public const string Earth = "ok_bp_earth";
        public const string StoneBack = "ok_bp_stoneback";
        public const string Materials = "ok_bp_materials";
        public const string Free = "ok_bp_free";
        public const string Missing = "ok_bp_missing";
        public const string Unloaded = "ok_bp_unloaded";
        public const string InTheWay = "ok_bp_intheway";
        public const string Unlearned = "ok_bp_unlearned";
        public const string Lacking = "ok_bp_lacking";
        public const string Warded = "ok_bp_warded";
        public const string NoBuild = "ok_bp_nobuild";
        public const string Indoors = "ok_bp_indoors";

        public const string Aim = "ok_bp_aim";
        public const string ClickToPin = "ok_bp_clicktopin";
        public const string Pinned = "ok_bp_pinned";
        public const string Unpinned = "ok_bp_unpinned";
        public const string Outline = "ok_bp_outline";
        public const string Progress = "ok_bp_progress";
        public const string Done = "ok_bp_done";
        public const string Limited = "ok_bp_limited";
        public const string Cleared = "ok_bp_cleared";

        public const string FixName = "ok_fix";
        public const string FixDescription = "ok_fix_desc";
        public const string FixTitle = "ok_fix_title";
        public const string FixAim = "ok_fix_aim";
        public const string FixClick = "ok_fix_click";
        public const string FixPinned = "ok_fix_pinned";
        public const string FixNothing = "ok_fix_nothing";
        public const string FixFine = "ok_fix_fine";
        public const string FixDone = "ok_fix_done";
        public const string FixUndone = "ok_fix_undone";
        public const string FixNoUndo = "ok_fix_noundo";
        public const string PlannerName = "ok_planner";
        public const string PlannerDescription = "ok_planner_desc";
        public const string CopyName = "ok_copy";
        public const string CopyDescription = "ok_copy_desc";

        public const string Tab = "ok_bp_tab";
        public const string EntrySize = "ok_bp_entry_size";
        public const string EntryWater = "ok_bp_entry_water";
        public const string EntryKeys = "ok_bp_entry_keys";

        public const string TopFolder = "ok_bp_top";
        public const string NewFolderName = "ok_bp_newfolder";
        public const string NewFolderDescription = "ok_bp_newfolder_desc";
        public const string NewFolderTopic = "ok_bp_newfolder_topic";
        public const string FolderMade = "ok_bp_foldermade";
        public const string RenameTopic = "ok_bp_rename_topic";
        public const string Renamed = "ok_bp_renamed";
        public const string BadName = "ok_bp_badname";
        public const string Exists = "ok_bp_exists";
        public const string IntoItself = "ok_bp_intoitself";
        public const string Moved = "ok_bp_moved";
        public const string MoveRefused = "ok_bp_moverefused";

        public const string GhostsName = "ok_bp_ghosts";
        public const string GhostsShownDescription = "ok_bp_ghosts_shown";
        public const string GhostsHiddenDescription = "ok_bp_ghosts_hidden";
        public const string GhostsShownBand = "ok_bp_ghosts_band_shown";
        public const string GhostsHiddenBand = "ok_bp_ghosts_band_hidden";
        public const string GhostsOn = "ok_bp_ghosts_on";
        public const string GhostsOff = "ok_bp_ghosts_off";

        /// <summary>A length or height as shown to players: one decimal.</summary>
        public static string Metres(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
