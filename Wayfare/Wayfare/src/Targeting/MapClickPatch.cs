using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Targeting
{
    /// <summary>Left click on one of Wayfare's own icons targets that portal instead of running Minimap's own pin
    /// click-to-toggle; right click favourites/unfavourites it instead of removing a real placed pin. Both
    /// prefixes only intercept when the click actually landed on one of our icons - everywhere else the vanilla
    /// map keeps working exactly as it always has.</summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    public static class MapLeftClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // The sea gate picker owns the map while open: a click on one of its icons never reaches portal
            // targeting or the game's pin toggle; anywhere else the game's own click runs.
            if (SeaGates.SeaGatePicker.Active)
                return !SeaGates.SeaGatePicker.TryClick(ZInput.pointerPosition);
            if (!WayfareConfig.Enabled.Value || !MapOverlay.TryHitTest(ZInput.pointerPosition, out ZDOID hit))
                return true;
            if (TargetingSession.Active)
                TargetingSession.Select(hit);
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), "RemovePinUnderPointer")]
    public static class MapRightClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (!WayfareConfig.Enabled.Value || !MapOverlay.TryHitTest(ZInput.pointerPosition, out ZDOID hit))
                return true;
            bool on = PlayerFavourites.Toggle(hit);
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, on ? Words.Favourited : Words.Unfavourited);
            return false;
        }
    }
}
