using UnityEngine;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// When the tracker shows: it is on, something is tracked, the player is alive, the large map is closed (Hide With
    /// Map) and no creature has been after the player for 5 seconds (Hide In Combat; the game's own "targeted" state,
    /// which a creature sets while it has the player as its target). The HUD's own hiding applies on top.
    /// </summary>
    public static class TrackerVisibility
    {
        private const float CombatLinger = 5f;

        private static float lastThreat = -100f;

        public static bool Show()
        {
            Player player = Player.m_localPlayer;
            if (!TrackerSettings.Enabled.Value || player == null || player.IsDead())
                return false;
            bool threatened = player.IsTargeted();
            if (threatened)
                lastThreat = Time.time;
            if (TrackerList.Entries.Count == 0)
                return false;
            if (TrackerSettings.HideWithMap.Value && Minimap.IsOpen())
                return false;
            return !TrackerSettings.HideInCombat.Value || Time.time - lastThreat >= CombatLinger;
        }

        /// <summary>The game has freed the cursor (inventory, map or a menu open), the way its camera decides.</summary>
        public static bool CursorFree => ZCursor.LockState != CursorLockMode.Locked && ZCursor.IsVisible;
    }
}
