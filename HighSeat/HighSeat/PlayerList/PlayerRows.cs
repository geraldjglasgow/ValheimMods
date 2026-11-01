using System;
using System.Collections.Generic;
using HarmonyLib;
using Splatform;
using UnityEngine;
using UnityEngine.Rendering;

namespace HighSeat
{
    /// <summary>
    /// The server's player list (ZNet.UpdatePlayerList, every 2 s from ZNet.SendPlayerList, and at each join and leave),
    /// built row for row as the game builds it but without garbage. The game clears ZNet.m_players and adds, in this
    /// order: the server's own player when it has a screen (a hosted game; a dedicated server's graphics device is Null):
    /// its character name, the platform's local user id and display name, its PlayFab id (or "" without PlayFab), its
    /// character id, and its position when it shares it; then each ready peer (ZNetPeer.IsReady, in ZNet.m_peers' order):
    /// the peer's player name and character id, its platform id (<see cref="PeerIds"/>, the game's own value), its
    /// "platformDisplayName" (or "" before the player's game has sent it), its PlayFab id, and its position when shared.
    /// Then each row gets its server-assigned name from ZNet.GetUniqueDisplayName (<see cref="FastNames"/> when on), and
    /// ZNet.UpdatePlayerHistory runs (<see cref="FastHistory"/> when on), both called as the game calls them, so the
    /// list, its order and every field are the game's, and every player is sent the same list. Server only, while the
    /// game's code is what it was checked against and no other mod changes it: a postfix of another mod on it still runs
    /// after this one, on the same list (ServerDevcommands' private positions for admins), anything else leaves the game's
    /// own method running (<see cref="ListChecks"/>); on any error, the game's own method for the rest of the session.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.UpdatePlayerList))]
    internal static class PlayerRows
    {
        private const string DisplayNameKey = "platformDisplayName";

        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(ZNet __instance, bool __runOriginal)
        {
            if (!__runOriginal || !ListGate.Active(ListGate.Part.Rows, __instance))
            {
                return true;
            }
            try
            {
                Build(__instance);
                Name(__instance);
                __instance.UpdatePlayerHistory();
                return false;
            }
            catch (Exception e)
            {
                ListGate.Fail(ListGate.Part.Rows, e);
                return true;
            }
        }

        private static void Build(ZNet net)
        {
            net.m_players.Clear();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                net.m_players.Add(Host(net));
            }
            foreach (ZNetPeer peer in net.m_peers)
            {
                if (peer.IsReady())
                {
                    net.m_players.Add(Row(net, peer));
                }
            }
        }

        private static ZNet.PlayerInfo Host(ZNet net)
        {
            ZNet.PlayerInfo row = new ZNet.PlayerInfo
            {
                m_name = Game.instance.GetPlayerProfile().GetName(),
                m_characterID = net.m_characterID,
                m_publicPosition = net.m_publicReferencePosition,
            };
            row.m_userInfo.m_id = LocalIds.Id();
            row.m_userInfo.m_displayName = PlatformManager.DistributionPlatform.LocalUser.DisplayName;
            row.m_userInfo.m_playfabId =
                (object)PlayFabManager.instance != null ? PlayFabManager.instance.PlayFabUniqueId : string.Empty;
            if (row.m_publicPosition)
            {
                row.m_position = net.m_referencePosition;
            }
            return row;
        }

        private static ZNet.PlayerInfo Row(ZNet net, ZNetPeer peer)
        {
            ZNet.PlayerInfo row = new ZNet.PlayerInfo { m_name = peer.m_playerName, m_characterID = peer.m_characterID };
            row.m_userInfo.m_id = PeerIds.Of(net, peer);
            row.m_userInfo.m_displayName =
                peer.m_serverSyncedPlayerData.TryGetValue(DisplayNameKey, out string name) ? name : "";
            row.m_userInfo.m_playfabId = peer.m_playfabId;
            row.m_publicPosition = peer.m_publicRefPos;
            if (row.m_publicPosition)
            {
                row.m_position = peer.m_refPos;
            }
            return row;
        }

        // The game's own loop: each row's server-assigned name, from the game's method (patched or not).
        private static void Name(ZNet net)
        {
            List<ZNet.PlayerInfo> players = net.m_players;
            for (int i = 0; i < players.Count; i++)
            {
                ZNet.PlayerInfo row = players[i];
                row.m_userInfo.m_serverAssignedDisplayName = net.GetUniqueDisplayName(row.m_userInfo);
                players[i] = row;
            }
        }
    }
}
