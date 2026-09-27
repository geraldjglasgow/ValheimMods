using EarthWright.Core;
using HarmonyLib;

namespace EarthWright.History
{
    /// <summary>
    /// The history, the snapshots and the send times belong to one world session: they are dropped when a world starts
    /// (so nothing carries over into another world) and when it ends at logout or disconnect. Death keeps them.
    /// </summary>
    internal static class Session
    {
        public static void Clear()
        {
            Timeline.Clear();
            Snapshots.Clear();
            SendTracker.Clear();
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class HistoryGameStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => Safe.Run("EarthWright history reset", Session.Clear);
    }

    [HarmonyPatch(typeof(Game), nameof(Game.OnDestroy))]
    public static class HistoryGameEndPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => Safe.Run("EarthWright history reset", Session.Clear);
    }
}
