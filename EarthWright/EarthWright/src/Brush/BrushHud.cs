using EarthWright.Actions;
using EarthWright.Core;

namespace EarthWright.Brush
{
    /// <summary>
    /// Hands the brush's lines to the HUD block ("brush" and "target" in <see cref="HudText"/>), which the Preview module
    /// draws. The keys are listed once, in the selected entry's description (the Menu module). Local display only.
    /// </summary>
    public static class BrushHud
    {
        public const int BrushOrder = 10;
        public const int TargetOrder = 20;

        public static void Show(ToolAction action, BrushValues values)
        {
            HudText.Set("brush", BrushHudText.BrushLine(action, values), BrushOrder);
            HudText.Set("target", BrushHudText.TargetLine(action, values), TargetOrder);
        }

        public static void Hide()
        {
            HudText.Clear("brush");
            HudText.Clear("target");
        }
    }
}
