using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;

namespace HighSeat
{
    /// <summary>
    /// The Steam send rate, set the way the game sets its own 150 KB/s (ZSteamSocket.RegisterGlobalCallbacks, the first time
    /// a socket is made): the same two global values, after the game's. Left alone at the game's own value.
    /// </summary>
    [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.RegisterGlobalCallbacks))]
    internal static class SendRate
    {
        private const int GameRate = 153600;

        private static void Postfix()
        {
            int rate = Settings.SendRateKBs.Value * 1024;
            if (rate == GameRate)
            {
                return;
            }
            GCHandle handle = GCHandle.Alloc(rate, GCHandleType.Pinned);
            try
            {
                Set(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, handle.AddrOfPinnedObject());
                Set(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private static void Set(ESteamNetworkingConfigValue value, IntPtr data) =>
            SteamNetworkingUtils.SetConfigValue(value, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, data);
    }
}
