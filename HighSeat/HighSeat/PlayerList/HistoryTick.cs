using System;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The player history's 2 s beat, after each ZNet.SendPlayerList (the game's player list send: every 2 s from
    /// ZNet.SendPeriodicData, and at each join and leave): the players online are noted as seen now
    /// (<see cref="SeenPlayers"/>) and players holding an older history are sent the newest when it is due
    /// (<see cref="HistorySend.Spread"/>). Nothing of the game's is replaced, so it runs beside any other mod's patch on
    /// the method; it does anything only while the history send is on (<see cref="ListGate.Part.Send"/>), and an error
    /// turns that off for the session (the game then sends the whole history as it does).
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SendPlayerList))]
    internal static class HistoryTick
    {
        private static void Postfix(ZNet __instance)
        {
            if (!ListGate.Active(ListGate.Part.Send, __instance))
            {
                return;
            }
            try
            {
                SeenPlayers.Touch(__instance.m_players);
                HistorySend.Spread(__instance, Time.unscaledTime);
            }
            catch (Exception e)
            {
                ListGate.Fail(ListGate.Part.Send, e);
            }
        }
    }
}
