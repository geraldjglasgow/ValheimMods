using HarmonyLib;
using MapClicks;
using Wayfare.Core;

namespace Wayfare.Targeting
{
    /// <summary>While choosing a destination, a left click on one of Wayfare's icons (its click area, the nearest when
    /// two overlap) targets that portal instead of running Minimap's own pin click-to-toggle; right click
    /// favourites/unfavourites it instead of removing a real placed pin. Both leave the game's pins under an icon
    /// working (the user's ask, 2026-10-04): the travel waits out the game's double click window, so a double click
    /// places a pin there instead (<see cref="IconClick.Hold"/>), and a right click on a placed pin removes the pin
    /// first. Outside targeting a left click is the game's. Everywhere else the vanilla map keeps working exactly as
    /// it always has.</summary>
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
            if (!WayfareConfig.PortalsOn || !TargetingSession.Active || !MapOverlay.TryHitTest(ZInput.pointerPosition, out ZDOID hit))
                return true;
            IconClick.Hold(() => TargetingSession.Select(hit));
            return false;
        }
    }

    [HarmonyPatch(typeof(Minimap), "RemovePinUnderPointer")]
    public static class MapRightClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (!WayfareConfig.PortalsOn || !MapOverlay.TryHitTest(ZInput.pointerPosition, out ZDOID hit))
                return true;
            if (IconClick.PinUnderPointer(Minimap.instance))
                return true;
            bool on = PlayerFavourites.Toggle(hit);
            if (Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, on ? Words.Favourited : Words.Unfavourited);
            return false;
        }
    }
}
