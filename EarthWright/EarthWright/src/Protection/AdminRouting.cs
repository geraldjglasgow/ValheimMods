using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// Sender side (<see cref="EditEvents.Building"/>): gives an admin's edit the flags that make the server vouch for it.
    /// <list type="bullet">
    /// <item>While the admin holds the limit override key (and, if set, is in god mode) the edit becomes
    /// <c>Privileged | IgnoreLimits</c>: the height limits do not apply to it.</item>
    /// <item>Otherwise, while a rule lets admins through that the owner of the ground cannot check by itself (lock bypass,
    /// admins-only tools, zone bypass), the edit becomes <c>Privileged</c>: it travels through the server, which checks
    /// the admin list, and reaches the owner approved.</item>
    /// </list>
    /// Non-admins' edits are left alone; a forged flag is dropped by the server relay or the owner handler anyway.
    /// </summary>
    public static class AdminRouting
    {
        public static void OnBuilding(TerrainEdit edit)
        {
            if (edit == null || !Side.LocalIsAdmin)
                return;
            if (OverrideActive)
            {
                edit.Flags |= EditFlags.Privileged | EditFlags.IgnoreLimits;
                return;
            }
            if (NeedsApproval)
                edit.Flags |= EditFlags.Privileged;
        }

        /// <summary>The local player is an admin holding the limit override key (in god mode when that is required).</summary>
        public static bool OverrideActive
        {
            get
            {
                if (!Side.LocalIsAdmin || !Keys.Held(ProtectionSettings.OverrideKey))
                    return false;
                if (!ProtectionSettings.OverrideNeedsGodMode.Value)
                    return true;
                Player player = Player.m_localPlayer;
                return player != null && player.InGodMode();
            }
        }

        /// <summary>A rule is active that admins pass only with the server's approval.</summary>
        private static bool NeedsApproval
        {
            get
            {
                if (TerrainLock.Locked && ProtectionSettings.AdminsBypassLock.Value)
                    return true;
                if (ProtectionSettings.ToolsAllowed.Value == ToolAccess.AdminsOnly)
                    return true;
                return ProtectionSettings.Zones.Value != ZoneMode.Off && ProtectionSettings.AdminsBypassZones.Value;
            }
        }
    }
}
