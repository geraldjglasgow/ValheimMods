using OpenKeep.Core;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The Site planner's words (keys "ok_planner_*"; the entry's own name and description are in
    /// <see cref="BlueprintWords"/>). Texts with numbers carry $1, $2 placeholders that <see cref="BlueprintWords.Format"/> fills.
    /// </summary>
    public static class PlannerWords
    {
        public static string SiteTitle { get; private set; }
        public static string Aiming { get; private set; }
        public static string AimingSelected { get; private set; }
        public static string HousePreview { get; private set; }
        public static string SameTypePreview { get; private set; }
        public static string SameTypeAdded { get; private set; }
        public static string SameTypeRemoved { get; private set; }
        public static string HouseTypePreview { get; private set; }
        public static string HouseTypeAdded { get; private set; }
        public static string HouseTypeRemoved { get; private set; }
        public static string AimHint { get; private set; }
        public static string SelectionLine { get; private set; }
        public static string Supports { get; private set; }
        public static string SelectionBuilt { get; private set; }
        public static string Free { get; private set; }
        public static string Keys { get; private set; }
        public static string SelectionName { get; private set; }
        public static string HouseName { get; private set; }
        public static string Queued { get; private set; }
        public static string QueueFull { get; private set; }
        public static string NothingNew { get; private set; }
        public static string Cleared { get; private set; }
        public static string HouseAdded { get; private set; }
        public static string HouseRemoved { get; private set; }
        public static string PanelTitle { get; private set; }
        public static string PanelNoSite { get; private set; }
        public static string PanelEmpty { get; private set; }
        public static string PanelKeys { get; private set; }
        public static string Progress { get; private set; }
        public static string RowSizes { get; private set; }
        public static string RowDone { get; private set; }
        public static string Up { get; private set; }
        public static string Down { get; private set; }
        public static string Remove { get; private set; }

        /// <summary>", +5 to hold them up" for the unbuilt supports a selection brings; empty when it brings none.</summary>
        public static string SupportsNote(int supports) => supports > 0 ? BlueprintWords.Format(Supports, supports) : "";

        public static void Register()
        {
            SiteTitle = Language.Add("ok_planner_site", "Site $1: $2 of $3 pieces built");
            Aiming = Language.Add("ok_planner_aiming", "Aiming at: $1");
            AimingSelected = Language.Add("ok_planner_aiming_selected", "Aiming at: $1 (selected)");
            HousePreview = Language.Add("ok_planner_house_preview", "- Shift + click selects its house: $1 pieces");
            SameTypePreview = Language.Add("ok_planner_sametype_preview", "- G + click selects the $1 joined pieces of this type");
            HouseTypePreview = Language.Add("ok_planner_housetype_preview", "- Shift + G + click selects all $1 pieces of this type in its house");
            AimHint = Language.Add("ok_planner_aim", "Aim at a ghost piece of a construction site");
            SelectionLine = Language.Add("ok_planner_selection_line", "Selection: $1 pieces$3 - $2");
            Supports = Language.Add("ok_planner_supports", ", +$1 to hold them up");
            SelectionBuilt = Language.Add("ok_planner_selection_built", "all built");
            Free = Language.Add("ok_planner_free", "free");
            Keys = Language.Add("ok_planner_keys", "<color=yellow>Click</color> select a piece  <color=yellow>Shift + click</color> its whole house  " +
                "<color=yellow>G + click</color> its joined pieces of the same type  " +
                "<color=yellow>Shift + G + click</color> every piece of that type in the house  " +
                "<color=yellow>Enter</color> queue the selection  <color=yellow>Backspace</color> clear it  <color=yellow>K</color> build queue");
            AddQueue();
            AddPanel();
        }

        private static void AddQueue()
        {
            SelectionName = Language.Add("ok_planner_name_selection", "Selection $1");
            HouseName = Language.Add("ok_planner_name_house", "House $1");
            Queued = Language.Add("ok_planner_queued", "Queued $1: $2 pieces are built first");
            QueueFull = Language.Add("ok_planner_full", "The build queue is full ($1 entries)");
            NothingNew = Language.Add("ok_planner_nothingnew", "Every selected piece is built or queued already");
            Cleared = Language.Add("ok_planner_cleared", "Selection cleared");
            HouseAdded = Language.Add("ok_planner_house_added", "House selected: $1 pieces added");
            HouseRemoved = Language.Add("ok_planner_house_removed", "House let go: $1 pieces");
            SameTypeAdded = Language.Add("ok_planner_sametype_added", "$1 joined pieces of this type selected");
            SameTypeRemoved = Language.Add("ok_planner_sametype_removed", "$1 joined pieces of this type let go");
            HouseTypeAdded = Language.Add("ok_planner_housetype_added", "$1 pieces of this type in the house selected");
            HouseTypeRemoved = Language.Add("ok_planner_housetype_removed", "$1 pieces of this type in the house let go");
        }

        private static void AddPanel()
        {
            PanelTitle = Language.Add("ok_planner_panel", "Build queue");
            PanelNoSite = Language.Add("ok_planner_panel_nosite", "No construction site within $1 m.");
            PanelEmpty = Language.Add("ok_planner_panel_empty", "Nothing queued. Select ghost pieces with the Site planner and press Enter: " +
                "queued pieces are built first, in this order.");
            PanelKeys = Language.Add("ok_planner_panel_keys", "Hover an entry to see its pieces. K or Esc closes.");
            Progress = Language.Add("ok_planner_progress", "$1 of $2 pieces built");
            RowSizes = Language.Add("ok_planner_row", "$1 pieces, $2 to build");
            RowDone = Language.Add("ok_planner_row_done", "Built");
            Up = Language.Add("ok_planner_up", "Up");
            Down = Language.Add("ok_planner_down", "Down");
            Remove = Language.Add("ok_planner_remove", "Remove");
        }
    }
}
