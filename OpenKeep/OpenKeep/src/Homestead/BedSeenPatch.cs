using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Bed.Awake</c> postfix on every client that loads a bed (a dedicated server keeps no list): a bed of the
    /// local player is remembered, so beds claimed before the mod or on another machine count once they have loaded
    /// nearby; any other bed at a remembered point (someone else's, or an unclaimed bed built where the player's
    /// stood) is forgotten. The ZDO arrives with its data, so the owner is known here.
    /// </summary>
    [HarmonyPatch(typeof(Bed), nameof(Bed.Awake))]
    public static class BedSeenPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Bed __instance)
        {
            if (!BedChecks.IsLive(__instance) || BedStore.Scope() == null)
                return;
            if (BedChecks.IsLocal(__instance))
                BedList.Remember(__instance.GetSpawnPoint());
            else
                BedList.Forget(__instance.GetSpawnPoint());
        }
    }
}
