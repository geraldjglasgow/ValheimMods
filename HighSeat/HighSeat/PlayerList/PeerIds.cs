using System.Collections.Generic;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// A player's platform id as the game's ZNet.UpdatePlayerList gives it, without a new string each time. The game asks
    /// the player's socket for its host name every 2 s: on Steam (backend Steamworks) the id is the Steam platform with
    /// ZSteamSocket.GetHostName(), the Steam ID's digits (a new string at each call); on crossplay (PlayFab) it is
    /// PlatformUserID parsed from ZPlayFabSocket.GetHostName(), the socket's own platform id written out (a new string,
    /// then two more for the parse); with any other backend, no id. Both host names depend on nothing but one value in
    /// the socket (the Steam ID, the socket's PlatformUserID), so here the game's own answer is kept per value and given
    /// back whenever the socket holds that value again: the same id, the same text. A socket of another type (a mod's) or
    /// a GetHostName another mod patches (<see cref="ListGate.Part.HostNames"/>) is asked every time, as the game does.
    /// The kept answers never go stale (each is the game's answer for its key); they grow by one per player seen. The
    /// server's own player (a hosted game) has its id from the platform's local user (<see cref="LocalIds"/>).
    /// </summary>
    internal static class PeerIds
    {
        private static readonly Dictionary<ulong, string> SteamNames = new Dictionary<ulong, string>();
        private static readonly Dictionary<PlatformUserID, PlatformUserID> PlayFabIds =
            new Dictionary<PlatformUserID, PlatformUserID>(IdKeys.Comparer);

        public static PlatformUserID Of(ZNet net, ZNetPeer peer)
        {
            switch (ZNet.m_onlineBackend)
            {
                case OnlineBackendType.Steamworks:
                    return new PlatformUserID(net.m_steamPlatform, SteamName(net, peer.m_socket));
                case OnlineBackendType.PlayFab:
                    return PlayFab(net, peer.m_socket);
                default:
                    return PlatformUserID.None;
            }
        }

        private static string SteamName(ZNet net, ISocket socket)
        {
            if (!(socket is ZSteamSocket steam) || steam.GetType() != typeof(ZSteamSocket) ||
                !ListGate.Active(ListGate.Part.HostNames, net))
            {
                return socket.GetHostName();
            }
            ulong id = steam.m_peerID.GetSteamID().m_SteamID;
            if (!SteamNames.TryGetValue(id, out string name))
            {
                name = steam.GetHostName();
                SteamNames[id] = name;
            }
            return name;
        }

        // An id that is not valid writes out as "", which the game's parse logs as a failure: asked every time, as the
        // game does.
        private static PlatformUserID PlayFab(ZNet net, ISocket socket)
        {
            if (!(socket is ZPlayFabSocket playFab) || playFab.GetType() != typeof(ZPlayFabSocket) ||
                !ListGate.Active(ListGate.Part.HostNames, net))
            {
                return new PlatformUserID(socket.GetHostName());
            }
            PlatformUserID from = playFab.m_platformPlayerId;
            if (from.IsValid && PlayFabIds.TryGetValue(from, out PlatformUserID id))
            {
                return id;
            }
            id = new PlatformUserID(socket.GetHostName());
            if (from.IsValid)
            {
                PlayFabIds[from] = id;
            }
            return id;
        }
    }
}
