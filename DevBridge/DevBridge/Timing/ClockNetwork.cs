using DevBridge.Server;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Timing
{
    /// <summary>
    /// Who else a held clock reaches. Time.timeScale is this machine's alone: everything it simulates slows, and its world
    /// updates to others are sent on game time, so a paused machine sends none.
    /// </summary>
    internal static class ClockNetwork
    {
        /// <summary>A dedicated server; the graphics check covers a server build whatever IsDedicated says.</summary>
        internal static bool Dedicated =>
            ZNet.instance && (ZNet.instance.IsDedicated() || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);

        /// <summary>Connected machines: other players for a server, the server for a client.</summary>
        internal static int Peers => ZNet.instance ? ZNet.instance.GetPeerConnections() : 0;

        internal static string Role()
        {
            ZNet net = ZNet.instance;
            if (!net) return "none";
            if (Dedicated) return "dedicated server";
            if (!net.IsServer()) return "client";
            return Peers > 0 || !ZNet.IsSinglePlayer ? "host" : "single player";
        }

        /// <summary>Throws unless the clock may be held here: a world is loaded, and a dedicated server needs force.</summary>
        internal static void Check(bool force)
        {
            if (!Game.instance || !ZNet.instance) throw new BridgeException("no world is loaded: the clock can only be held in one");
            if (Dedicated && !force)
                throw new BridgeException("this is a dedicated server: its clock runs the world for every player (slowed, they see it slow; " +
                                          "paused, it sends them no world updates). Add force=1 to do it anyway");
        }

        /// <summary>What drifts while the clock is held with others connected, or null when nobody else is affected.</summary>
        internal static string Warning()
        {
            ZNet net = ZNet.instance;
            if (!net) return null;
            if (Dedicated) return "dedicated server: every player's world runs on this clock; while paused the server sends them no world updates";
            if (!net.IsServer())
                return "client: only this machine's clock changes. The server and the other players run on, so what this machine " +
                       "simulates (its player, creatures near it) slows or stops for them and the rest drifts out of step; while " +
                       "paused it sends no world updates";
            int peers = Peers;
            return peers == 0 ? null :
                $"host with {peers} other player(s) connected: their games run on at normal speed, so what this host simulates " +
                "slows or stops for them and the rest drifts out of step; while paused it sends them no world updates";
        }
    }
}
