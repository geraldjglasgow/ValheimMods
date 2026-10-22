using Translations;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The keys of the Site planner's words (<c>ok_planner_*</c>; the entry's own name and description are in
    /// <see cref="BlueprintWords"/>). Their English is in <c>translations/English.blueprints.yml</c>; texts with values
    /// carry $1, $2 placeholders for <c>Lang.Format</c>.
    /// </summary>
    public static class PlannerWords
    {
        public const string SiteTitle = "ok_planner_site";
        public const string Aiming = "ok_planner_aiming";
        public const string AimingSelected = "ok_planner_aiming_selected";
        public const string HousePreview = "ok_planner_house_preview";
        public const string SameTypePreview = "ok_planner_sametype_preview";
        public const string HouseTypePreview = "ok_planner_housetype_preview";
        public const string AimHint = "ok_planner_aim";
        public const string SelectionLine = "ok_planner_selection_line";
        public const string Supports = "ok_planner_supports";
        public const string SelectionBuilt = "ok_planner_selection_built";
        public const string Free = "ok_planner_free";
        public const string Keys = "ok_planner_keys";

        public const string SelectionName = "ok_planner_name_selection";
        public const string HouseName = "ok_planner_name_house";
        public const string Queued = "ok_planner_queued";
        public const string QueueFull = "ok_planner_full";
        public const string NothingNew = "ok_planner_nothingnew";
        public const string Cleared = "ok_planner_cleared";
        public const string HouseAdded = "ok_planner_house_added";
        public const string HouseRemoved = "ok_planner_house_removed";
        public const string SameTypeAdded = "ok_planner_sametype_added";
        public const string SameTypeRemoved = "ok_planner_sametype_removed";
        public const string HouseTypeAdded = "ok_planner_housetype_added";
        public const string HouseTypeRemoved = "ok_planner_housetype_removed";

        public const string PanelTitle = "ok_planner_panel";
        public const string PanelNoSite = "ok_planner_panel_nosite";
        public const string PanelEmpty = "ok_planner_panel_empty";
        public const string PanelKeys = "ok_planner_panel_keys";
        public const string Progress = "ok_planner_progress";
        public const string RowSizes = "ok_planner_row";
        public const string RowDone = "ok_planner_row_done";
        public const string Up = "ok_planner_up";
        public const string Down = "ok_planner_down";
        public const string Remove = "ok_planner_remove";

        /// <summary>", +5 to hold them up" for the unbuilt supports a selection brings; empty when it brings none.</summary>
        public static string SupportsNote(int supports) => supports > 0 ? Lang.Format(Supports, supports) : "";
    }
}
