using EarthWright.Core;

namespace EarthWright.Preview
{
    /// <summary>
    /// The English words of the preview, the HUD and the panel ("ew_preview_..." keys). The names of shapes, styles,
    /// paints and target modes are the Brush module's own words, so the panel and the HUD always agree.
    /// </summary>
    internal static class PreviewWords
    {
        public static void Register()
        {
            Hud();
            Panel();
        }

        private static void Hud()
        {
            Language.Add("ew_preview_points", "points");
            Language.Add("ew_preview_cells", "paint cells");
            Language.Add("ew_preview_limit", "height limit reached");
            Language.Add("ew_preview_outofreach", "Out of reach");
            Language.Add("ew_preview_tile", "Tile");
            Language.Add("ew_preview_ground", "ground");
            Language.Add("ew_preview_edited", "edited");
            Language.Add("ew_preview_totarget", "to target");
            Language.Add("ew_preview_locked", "EarthWright settings are locked by the server");
            Language.Add("ew_preview_grid_on", "World grid on");
            Language.Add("ew_preview_grid_off", "World grid off");
            Language.Add("ew_preview_menu_button", "EarthWright");
        }

        private static void Panel()
        {
            Language.Add("ew_preview_panel_title", "EarthWright");
            Language.Add("ew_preview_panel_brush", "Brush");
            Language.Add("ew_preview_panel_noentry", "Hold the hoe or cultivator and pick a terrain entry to change its brush here.");
            Language.Add("ew_preview_close", "Close");
            Language.Add("ew_preview_radius", "Radius (m)");
            Language.Add("ew_preview_rotation", "Rotation (°)");
            Language.Add("ew_preview_hardness", "Edge hardness (%)");
            Language.Add("ew_preview_amount", "Raise / lower amount (m)");
            Language.Add("ew_preview_maxstep", "Largest step per click (m)");
            Language.Add("ew_preview_strength", "Smoothing strength (%)");
            Language.Add("ew_preview_density", "Grass density (%)");
            Language.Add("ew_preview_target_height", "Target height (m)");
            Language.Add("ew_preview_lock", "Lock the target height");
            Language.Add("ew_preview_presets", "Presets (amount × radius)");
            Language.Add("ew_preview_shape", "Shape");
            Language.Add("ew_preview_style", "Level style");
            Language.Add("ew_preview_source", "Target height from");
            Language.Add("ew_preview_paint", "Paint");
        }
    }
}
