using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Protection
{
    /// <summary>
    /// Admin zones ("Admin Zone Mode"), checked twice: by the sender and again by the owner of the ground, both against
    /// the server's published zones (<see cref="ZoneBook.Current"/>).
    /// <list type="bullet">
    /// <item>OnlyInsideZones: every part of the footprint must lie inside a zone meant for the player.</item>
    /// <item>NeverInsideZones: no part may reach into a zone, unless the zone names this player.</item>
    /// </list>
    /// Admins pass when "Admins Bypass Zones" is on: on the sender by their own status, on the owner only when the server
    /// approved the edit.
    /// </summary>
    public static class ZoneGuard
    {
        private static ZoneMode Mode => ProtectionSettings.Zones.Value;

        public static string Sender(GuardContext ctx)
        {
            if (Mode == ZoneMode.Off || LocalBypass)
                return null;
            return Judge(Footprint.Of(ctx.Edit), LocalName());
        }

        public static string Owner(GuardContext ctx)
        {
            if (Mode == ZoneMode.Off || (ProtectionSettings.AdminsBypassZones.Value && ctx.ServerApproved))
                return null;
            return Judge(Footprint.Of(ctx.Edit), SenderNames.Of(ctx.Edit));
        }

        /// <summary>The local player is an admin and admins pass the zones.</summary>
        public static bool LocalBypass => ProtectionSettings.AdminsBypassZones.Value && Side.LocalIsAdmin;

        public static string LocalName()
        {
            Player player = Player.m_localPlayer;
            return player != null ? player.GetPlayerName() : null;
        }

        /// <summary>The refusal for these circles and this player (null name: unknown, see <see cref="AdminZone.Allows"/>).</summary>
        public static string Judge(List<Disc> discs, string playerName)
        {
            foreach (Disc disc in discs)
            {
                string reason = JudgeDisc(disc, playerName);
                if (reason != null)
                    return reason;
            }
            return null;
        }

        /// <summary>The refusal for one circle (a radius 0 circle is a point), or null; nothing while the mode is Off.</summary>
        public static string JudgeDisc(Disc disc, string playerName)
        {
            IReadOnlyList<AdminZone> zones = ZoneBook.Current;
            switch (Mode)
            {
                case ZoneMode.OnlyInsideZones:
                    return InsideAllowed(disc, zones, playerName) ? null : ProtectionWords.ZoneOutside;
                case ZoneMode.NeverInsideZones:
                    return TouchesForbidden(disc, zones, playerName) ? ProtectionWords.ZoneInside : null;
                default:
                    return null;
            }
        }

        private static bool InsideAllowed(Disc disc, IReadOnlyList<AdminZone> zones, string playerName)
        {
            foreach (AdminZone zone in zones)
            {
                if (zone.Allows(playerName) && zone.Contains(disc))
                    return true;
            }
            return false;
        }

        private static bool TouchesForbidden(Disc disc, IReadOnlyList<AdminZone> zones, string playerName)
        {
            foreach (AdminZone zone in zones)
            {
                bool exempt = !string.IsNullOrEmpty(zone.Player) && zone.Allows(playerName);
                if (!exempt && zone.Touches(disc))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Owner side: the character name of the player who sent an edit, for zones that name a player. Found from the
    /// player's character if it is loaded here, else from the game's player list (whose character ZDO was created by the
    /// sending machine); null when neither knows it.
    /// </summary>
    public static class SenderNames
    {
        public static string Of(TerrainEdit edit)
        {
            if (edit == null)
                return null;
            Player player = edit.SenderPlayer != 0L ? Player.GetPlayer(edit.SenderPlayer) : null;
            if (player != null)
                return player.GetPlayerName();
            if (ZNet.instance == null || edit.SenderPeer == 0L)
                return null;
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                if (!info.m_characterID.IsNone() && info.m_characterID.UserID == edit.SenderPeer)
                    return info.m_name;
            }
            return null;
        }
    }
}
