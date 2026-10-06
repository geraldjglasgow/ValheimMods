using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The copy-floor-height key is the middle mouse button by default, which is also the game's "Remove" button in
    /// build mode. Called from the local player's <c>Player.UpdatePlacement</c> prefix (the shared patch in
    /// <c>Patches/UpdatePlacementPatch</c>): while a terrain entry is selected and that key is held or released this
    /// frame, the game's remove press is blocked for the frame (its own <c>m_blockRemove</c> flag), so copying a floor's
    /// height never deconstructs it with a tool that can remove pieces.
    /// </summary>
    public static class FloorRemoveGuard
    {
        public static void Apply(Player player)
        {
            try
            {
                if (!BrushState.Active || TargetKeys.Floor == null)
                    return;
                KeyCode key = TargetKeys.Floor.Value.MainKey;
                if (key != KeyCode.None && (Input.GetKey(key) || Input.GetKeyUp(key)))
                    player.m_blockRemove = true;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("floor key remove guard", e);
            }
        }
    }
}
