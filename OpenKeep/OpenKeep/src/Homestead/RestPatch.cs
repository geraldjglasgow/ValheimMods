using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>SE_Cozy.UpdateStatusEffect</c> is the Resting effect's tick: it adds Rested once the effect's time passes
    /// <c>m_delay</c>. It runs only in <c>SEMan.Update</c> on the player's ZDO owner, the player's own client, on the copy
    /// the SEMan made when resting began. The prefix sets that copy's <c>m_delay</c> from the setting every tick, so a
    /// changed or synced value applies at once; the item database's asset is never changed.
    /// </summary>
    [HarmonyPatch(typeof(SE_Cozy), nameof(SE_Cozy.UpdateStatusEffect))]
    public static class RestPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SE_Cozy __instance)
        {
            if (RestDelay.IsResting(__instance))
                __instance.m_delay = RestDelay.Seconds();
        }
    }
}
