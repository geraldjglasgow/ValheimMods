using Wayfare.Core;
using Wayfare.Portals;
using Wayfare.Targeting;

namespace Wayfare.SeaGates
{
    /// <summary>The server's half of <see cref="SeaGateGrant"/>: whether ships may jump from one gate. The server holds
    /// every pillar ZDO, so it alone can find a distant destination, and it is the one party every client trusts to
    /// judge access - the same reasoning as <see cref="TeleportGate"/>. The destination is the gate the helmsman picked;
    /// access is that gate's, read from its anchor (which carries <c>wf_mode</c>/<c>wf_owner</c>) for the asking
    /// player, by the portals' own rule.</summary>
    internal static class SeaGateGrantServer
    {
        internal static void OnRequest(long sender, long sourceId, long destId, ZDOID shipId)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return;
            string denial = Evaluate(sender, sourceId, destId, shipId, out ZDO destAnchor, out ZDO destPartner);
            if (denial != null)
                SeaGateGrant.SendDenial(sender, sourceId, destId, denial);
            else
                SeaGateGrant.SendGrant(sender, sourceId, destId, destAnchor, destPartner);
        }

        /// <summary>Null when the jump is granted, otherwise the denial token to send back.</summary>
        private static string Evaluate(long sender, long sourceId, long destId, ZDOID shipId, out ZDO destAnchor, out ZDO destPartner)
        {
            destAnchor = null;
            destPartner = null;
            if (!WayfareConfig.Enabled.Value || !WayfareConfig.SeaGatesEnabled.Value)
                return SeaGateGrant.DeniedClosed;
            if (TeleportGate.Blocked(out string blockedReason))
                return blockedReason;
            if (!SeaGateIndex.TryFindOnServer(sourceId, out _, out _))
                return SeaGateGrant.DeniedClosed;
            if (destId == sourceId)
                return SeaGateWords.DeniedGone;
            // A gate with no side deep enough to exit on is never paired, but a ship must never be sent to one.
            if (!SeaGateIndex.TryFindOnServer(destId, out destAnchor, out destPartner) || SeaGateFields.GetSides(destAnchor) == 0)
                return SeaGateWords.DeniedGone;
            Asker(sender, shipId, out long playerId, out bool isAdmin);
            return AccessDenial(playerId, isAdmin, destAnchor);
        }

        /// <summary>The player access is judged for: the ship's helmsman (the ship's own <c>user</c> key, which its
        /// helm writes) when the asking machine owns that ship, else the asking player. A ship near the world centre can
        /// be owned by a dedicated server, which has no player of its own; a ship owned by someone on the dock is
        /// steered by someone else.</summary>
        private static void Asker(long sender, ZDOID shipId, out long playerId, out bool isAdmin)
        {
            playerId = SenderIdentity.PlayerId(sender);
            isAdmin = SenderIdentity.IsAdmin(sender);
            ZDO ship = shipId.IsNone() || ZDOMan.instance == null ? null : ZDOMan.instance.GetZDO(shipId);
            long helmsman = ship != null && ship.GetOwner() == sender ? ship.GetLong(ZDOVars.s_user, 0L) : 0L;
            if (helmsman == 0L || helmsman == playerId)
                return;
            playerId = helmsman;
            isAdmin = IsAdminPlayer(helmsman);
        }

        private static bool IsAdminPlayer(long playerId)
        {
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer != null && SenderIdentity.PeerPlayerId(peer) == playerId && peer.m_socket != null)
                    return ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            }
            return false;
        }

        /// <summary>Null when the asking player may sail to the destination; otherwise why not, by the same logic as
        /// the portals' denial reason (an Admin gate says so, anything else is the owner's).</summary>
        private static string AccessDenial(long playerId, bool isAdmin, ZDO destAnchor)
        {
            if (PortalAccess.MayTarget(destAnchor, playerId, isAdmin))
                return null;
            return PortalFields.GetMode(destAnchor) == PortalMode.Admin ? SeaGateGrant.DeniedAdmin : SeaGateGrant.DeniedPrivate;
        }
    }
}
