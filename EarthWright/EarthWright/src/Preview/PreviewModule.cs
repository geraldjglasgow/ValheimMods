using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Preview
{
    /// <summary>
    /// Entry point of the Preview module (section "9. Preview and HUD"): everything this player sees of the brush
    /// before clicking, and none of it changes the world. Outline, changed points, 3D volume, the game's ghost ring,
    /// piece highlight, world grid, the HUD next to the crosshair, the controls hint, the locked badge, dust removal
    /// and the panel. Each visual runs as its own guarded per-frame callback, so one failing does not stop the others;
    /// <see cref="PreviewFrame"/> goes first so they all draw the same edit.
    /// </summary>
    public static class PreviewModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            PreviewSettings.Bind(synced);
            HudSettings.Bind(synced);
            PreviewWords.Register();
            Keys.PanelTyping = () => PanelWindow.Typing;
            Ticker.OnUpdate("EarthWright preview frame", PreviewFrame.Update);
            Ticker.OnUpdate("EarthWright ghost visuals", GhostVisuals.Update);
            Ticker.OnUpdate("EarthWright outline", OutlinePreview.Update);
            Ticker.OnUpdate("EarthWright changed points", ChangedPoints.Update);
            Ticker.OnUpdate("EarthWright volume", VolumePreview.Update);
            Ticker.OnUpdate("EarthWright piece highlight", PieceHighlight.Update);
            Ticker.OnUpdate("EarthWright cursor readout", CursorReadout.Update);
            Ticker.OnUpdate("EarthWright world grid", WorldGrid.Update);
            Ticker.OnUpdate("EarthWright panel keys", PanelWindow.Update);
            Ticker.OnGui("EarthWright HUD", HudOverlay.OnGui);
            Ticker.OnGui("EarthWright panel", PanelWindow.OnGui);
        }
    }
}
