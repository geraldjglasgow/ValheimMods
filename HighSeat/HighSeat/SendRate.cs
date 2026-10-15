using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;

namespace HighSeat
{
    /// <summary>
    /// The Steam send rate raised to at least 500 KB/s per player, from the game's 150: set the way the game sets its own
    /// (ZSteamSocket.RegisterGlobalCallbacks, the first time a socket is made), right after it. Never lowered: a rate
    /// another mod already set higher (a network mod) is kept. No setting; a server that has trouble with it is a report.
    /// A dedicated server talks to Steam through the game-server interface, a player's game through the client one: the
    /// wrong one throws, and this runs while the game opens its socket, so nothing here may ever throw out of it (0.2.0
    /// called the client interface on dedicated servers, which then never listened).
    /// </summary>
    [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
    internal static class SendRate
    {
        private const int Rate = 500 * 1024;

        private static void Postfix()
        {
            try
            {
                Raise();
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"send rate left as it was: {e.Message}");
            }
        }

        private static void Raise()
        {
            IntPtr data = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                if (Current(data) >= Rate)
                {
                    return;
                }
                Marshal.WriteInt32(data, Rate);
                Set(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, data);
                Set(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, data);
            }
            finally
            {
                Marshal.FreeHGlobal(data);
            }
        }

        // A dedicated server (run in batch mode) speaks to Steam as a game server; a player's game, hosting or not, as a client.
        private static bool GameServer => UnityEngine.Application.isBatchMode;

        // The global maximum as it stands; 0 when Steam will not say.
        private static int Current(IntPtr data)
        {
            ulong size = sizeof(int);
            ESteamNetworkingConfigValue max = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax;
            ESteamNetworkingConfigScope global = ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global;
            ESteamNetworkingGetConfigValueResult result = GameServer
                ? SteamGameServerNetworkingUtils.GetConfigValue(max, global, IntPtr.Zero, out _, data, ref size)
                : SteamNetworkingUtils.GetConfigValue(max, global, IntPtr.Zero, out _, data, ref size);
            return result == ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_OK ? Marshal.ReadInt32(data) : 0;
        }

        private static void Set(ESteamNetworkingConfigValue value, IntPtr data)
        {
            ESteamNetworkingConfigScope global = ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global;
            ESteamNetworkingConfigDataType int32 = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
            if (GameServer)
            {
                SteamGameServerNetworkingUtils.SetConfigValue(value, global, IntPtr.Zero, int32, data);
                return;
            }
            SteamNetworkingUtils.SetConfigValue(value, global, IntPtr.Zero, int32, data);
        }
    }
}
