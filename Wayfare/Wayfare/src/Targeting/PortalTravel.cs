using UnityEngine;

namespace Wayfare.Targeting
{
    /// <summary>The client's end of a granted portal teleport, for whichever window asked: the TargetTeleport picker
    /// while it is open, else the map's targeting session. The destination comes from the server's grant, never from a
    /// local ZDO lookup - a client usually holds no ZDO at all for a distant portal.</summary>
    public static class PortalTravel
    {
        public static void Granted(Vector3 targetPos, Quaternion targetRot)
        {
            if (PortalPicker.IsOpen)
                PortalPicker.Complete(targetPos, targetRot);
            else
                TargetingSession.CompleteTeleport(targetPos, targetRot);
        }

        /// <summary>Steps the local player out of the destination portal the way the game's own
        /// <c>TeleportWorld.Teleport</c> does: the source portal's carry rule (<c>IsTeleportable</c>) and exit distance,
        /// the game's long teleport and its portal stat. False when there was no teleport.</summary>
        public static bool Go(TeleportWorld source, Vector3 targetPos, Quaternion targetRot)
        {
            Player player = Player.m_localPlayer;
            if (player == null || source == null)
                return false;
            if (!player.IsTeleportable(source.m_allowAllItems))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return false;
            }
            Vector3 exitPos = targetPos + targetRot * Vector3.forward * source.m_exitDistance + Vector3.up;
            player.TeleportTo(exitPos, targetRot, distantTeleport: true);
            Game.instance.IncrementPlayerStat(PlayerStatType.PortalsUsed);
            return true;
        }
    }
}
