using EarthWright.Core;

namespace EarthWright.Brush
{
    /// <summary>
    /// Messages about brush changes. Value steps (size, amount, ...) are shown in the middle of the screen only when
    /// "Announce Changes" is on, since the HUD already shows them; toggles (shape, style, paint, grid, target mode)
    /// get a short top-left note, as the game does for its own toggles.
    /// </summary>
    public static class BrushAnnounce
    {
        public static void Value(string text)
        {
            if (BrushSettings.Announce.Value && !string.IsNullOrEmpty(text))
                Messages.Center(text);
        }

        public static void Toggle(string text) => Messages.TopLeft(text);
    }
}
