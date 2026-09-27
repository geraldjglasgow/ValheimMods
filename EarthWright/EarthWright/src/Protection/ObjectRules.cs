using System;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>
    /// The protection rules for work that changes world objects rather than terrain (clearing, uproot, the clearing
    /// commands): these never pass the terrain guards, so the modules that remove objects ask here per object, on the
    /// local player's machine. The rules are the terrain guards' own: the terrain tools switch, the terrain lock (admins
    /// bypass as configured; exempt when the held item is on the list), the combat lock and the admin zones (with
    /// player-specific zones and the admin bypass). Wards, no-build places and dungeons stay with the callers, who check
    /// them already. Everything but the zones is the same for every object of a frame, so it is worked out once per
    /// frame (per tool); the zone test is a loop over at most 100 circles.
    /// </summary>
    public static class ObjectRules
    {
        private static int cachedFrame = -1;
        private static string cachedTool;
        private static Player cachedPlayer;
        private static string cachedReason;
        private static string cachedName;

        /// <summary>Null when the local player may remove or harvest objects at this position, else the refusal reason.</summary>
        public static string Refusal(Player player, Vector3 position, string toolItemName)
        {
            try
            {
                player = player != null ? player : Player.m_localPlayer;
                Refresh(player, toolItemName);
                if (cachedReason != null)
                    return cachedReason;
                if (ProtectionSettings.Zones.Value == ZoneMode.Off || ZoneGuard.LocalBypass)
                    return null;
                return ZoneGuard.JudgeDisc(new Disc(position, 0f), cachedName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("EarthWright object rules: " + e);
                return null;
            }
        }

        /// <summary>The position-independent reason and the player's name, once per frame, player and tool.</summary>
        private static void Refresh(Player player, string toolItemName)
        {
            int frame = Time.frameCount;
            if (frame == cachedFrame && player == cachedPlayer && string.Equals(toolItemName, cachedTool, StringComparison.Ordinal))
                return;
            cachedFrame = frame;
            cachedPlayer = player;
            cachedTool = toolItemName;
            cachedName = player != null ? player.GetPlayerName() : null;
            cachedReason = GeneralReason(toolItemName);
        }

        private static string GeneralReason(string toolItemName)
        {
            string reason = AccessGuards.ToolsReason(Side.LocalIsAdmin);
            if (reason != null)
                return reason;
            if (TerrainLock.BlocksLocal(toolItemName))
                return ProtectionWords.Locked;
            return CombatLock.Reason();
        }
    }
}
