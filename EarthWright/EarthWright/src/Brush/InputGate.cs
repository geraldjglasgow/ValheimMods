using EarthWright.Core;

namespace EarthWright.Brush
{
    /// <summary>
    /// Whether the brush may read the keyboard, wheel and gamepad now: a terrain entry is selected, nothing is being
    /// typed, and the game itself takes the player's input (<c>Player.TakeInput</c>: no chat, console, inventory, map,
    /// menu, store, text viewer, build-menu search or free camera, and not while the EarthWright panel is open, which
    /// the Preview module adds there), and the radial menu is closed.
    /// </summary>
    public static class InputGate
    {
        public static bool Open
        {
            get
            {
                Player player = Player.m_localPlayer;
                if (!BrushState.Active || player == null || Hud.instance == null || Keys.TextInputActive)
                    return false;
                return player.TakeInput() && !Hud.InRadial();
            }
        }
    }
}
