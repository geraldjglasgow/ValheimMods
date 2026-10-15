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
    /// </summary>
    [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
    internal static class SendRate
    {
        private const int Rate = 500 * 1024;

        private static void Postfix()
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

        // The global maximum as it stands; 0 when Steam will not say.
        private static int Current(IntPtr data)
        {
            ulong size = sizeof(int);
            ESteamNetworkingGetConfigValueResult result = SteamNetworkingUtils.GetConfigValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero, out _, data, ref size);
            return result == ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_OK ? Marshal.ReadInt32(data) : 0;
        }

        private static void Set(ESteamNetworkingConfigValue value, IntPtr data) =>
            SteamNetworkingUtils.SetConfigValue(value, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, data);
    }
}
