using System;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The Steam lobby a hosted game lists itself with shows the player cap. The game makes it for 10 members
    /// (ZSteamMatchmaking.RegisterServer); once Steam has made it (ZSteamMatchmaking.OnLobbyCreated, where the game sets
    /// the lobby's name and data) its member limit is set to Max players, at most Steam's own 250, and set again when the
    /// .cfg is reloaded. Nobody joins that lobby (players connect to the host's game, not the lobby), so the limit only
    /// changes the "players / limit" the server list shows; the cap itself is <see cref="PlayerCap"/>. Only on the Steam
    /// backend: a crossplay (PlayFab) game keeps its 10, and its PlayFab lobby is left alone. A dedicated server lists
    /// itself through Steam's game-server interface in its own build, which this does not touch (not patched in batch
    /// mode, nor when the game has no such method). Nothing here throws: Steam refusing leaves the game's 10.
    /// </summary>
    [HarmonyPatch(typeof(ZSteamMatchmaking), nameof(ZSteamMatchmaking.OnLobbyCreated))]
    internal static class LobbySize
    {
        private const int SteamMaxMembers = 250;
        private static ZSteamMatchmaking _matchmaking;
        private static bool _listening;

        private static bool Prepare()
        {
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string created = nameof(ZSteamMatchmaking.OnLobbyCreated);
            return !Application.isBatchMode && typeof(ZSteamMatchmaking).GetMethod(created, any) != null;
        }

        private static void Postfix(ZSteamMatchmaking __instance, LobbyCreated_t data, bool ioError)
        {
            if (ioError || data.m_eResult != EResult.k_EResultOK)
            {
                return;
            }
            _matchmaking = __instance;
            Apply();
            if (!_listening)
            {
                _listening = true;
                Settings.MaxPlayers.SettingChanged += (sender, args) => Apply();
            }
        }

        private static void Apply()
        {
            try
            {
                SetLimit();
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"Steam lobby left at the game's member limit: {e.Message}");
            }
        }

        private static void SetLimit()
        {
            if (_matchmaking == null || ZNet.m_onlineBackend != OnlineBackendType.Steamworks)
            {
                return;
            }
            CSteamID lobby = _matchmaking.m_myLobby;
            if (lobby == CSteamID.Nil)
            {
                return;
            }
            int limit = Mathf.Clamp(Settings.MaxPlayers.Value, 1, SteamMaxMembers);
            if (!SteamMatchmaking.SetLobbyMemberLimit(lobby, limit))
            {
                HighSeat.Log.LogWarning($"Steam refused a lobby member limit of {limit}: the server list shows the game's 10");
            }
        }
    }
}
