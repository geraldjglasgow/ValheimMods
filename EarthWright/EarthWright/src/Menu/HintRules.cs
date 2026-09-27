using EarthWright.Actions;
using EarthWright.Brush;

namespace EarthWright.Menu
{
    /// <summary>
    /// Which key hints an entry's description may show: the Brush module's own rules decide (its public predicates for
    /// the level style, the target keys and hard level), so a description never names a key the brush ignores for that
    /// entry. The brush leaves the shape, rotation, grid and value-selector keys to plain brush entries; special entries
    /// (ramp, road, clearing, uproot, groundbreaker, custom) only resize and pick a paint.
    /// </summary>
    public static class HintRules
    {
        public static bool Applies(HintKind kind, ToolAction action)
        {
            if (action == null)
                return true;
            switch (kind)
            {
                case HintKind.Style: return StyleCycle.Applies(action);
                case HintKind.Lock:
                case HintKind.Mode: return TargetHeight.Used(action);
                case HintKind.Hard: return HardLevel.Applies(action);
                case HintKind.Shape:
                case HintKind.Rotate:
                case HintKind.Grid:
                case HintKind.Next: return !action.IsPathTool;
                default: return true;
            }
        }

        /// <summary>What the size keys change: the selected value, the brush size of a special entry, a path's width.</summary>
        public static string AdjustWord(ToolAction action)
        {
            if (action == null || !action.IsSpecial)
                return "$ew_menu_hint_adjust";
            return action.Special == "ramp" || action.Special == "road" ? "$ew_menu_hint_width" : "$ew_menu_hint_size";
        }
    }
}
