using System;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The player limit a dedicated server shows in the game's server list ("players / limit"). The dedicated server's own
    /// build registers with Steam's game-server interface and tells it 10 (ZSteamMatchmaking.RegisterServer:
    /// SteamGameServer.SetMaxPlayerCount); right after, the limit is set to Max players instead, and again when the .cfg
    /// is reloaded, so the list shows the real limit (the cap itself is <see cref="PlayerCap"/>). Only on a dedicated
    /// server on the Steam backend: a hosted game lists itself through a lobby (<see cref="LobbySize"/>), and a crossplay
    /// server stays at the game's 10. Nothing here throws: a failure leaves the game's 10.
    /// </summary>
    [HarmonyPatch(typeof(ZSteamMatchmaking), nameof(ZSteamMatchmaking.RegisterServer))]
    internal static class ServerListSize
    {
        private static bool _listening;

        private static bool Prepare() => Application.isBatchMode;

        private static void Postfix()
        {
            Apply();
            if (!_listening)
            {
                _listening = true;
                Settings.MaxPlayers.SettingChanged += (sender, args) => Apply();
            }
        }

        private static void Apply()
        {
            if (ZNet.m_onlineBackend != OnlineBackendType.Steamworks)
            {
                return;
            }
            try
            {
                SteamGameServer.SetMaxPlayerCount(Mathf.Clamp(Settings.MaxPlayers.Value, 1, 1000));
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"the server list shows the game's 10 players: {e.Message}");
            }
        }
    }
}
