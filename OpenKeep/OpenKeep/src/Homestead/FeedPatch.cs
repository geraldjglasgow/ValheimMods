using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Smelter.UpdateSmelter</c> runs every second on every client that has the station loaded (an
    /// <c>InvokeRepeating</c> from <c>Awake</c>); only the ZDO owner works the queue in it. The postfix feeds on that
    /// owner after the game's work. Every other machine returns after the setting and the ownership check.
    /// </summary>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
    public static class FeedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Smelter __instance)
        {
            if (FeedSettings.AutoFeedStations.Value && FeedStations.OwnedPlayerStation(__instance))
                FeedTick.Tick(__instance);
        }
    }
}
