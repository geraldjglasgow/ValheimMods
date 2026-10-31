using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The world closing (ZDOMan.ShutDown, from ZNet.StopAll): the send loop stops recording changes and lets go of its
    /// players, so loading the next world (every object read sets an owner, which the hooks see) records nothing for
    /// nobody. The next world's first frame starts the loop afresh. Never throws into the game's shutdown.
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ShutDown))]
    internal static class SessionEnd
    {
        private static void Prefix()
        {
            try
            {
                SyncLoop.End();
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"send loop state left as it was at shutdown: {e.Message}");
            }
        }
    }
}
