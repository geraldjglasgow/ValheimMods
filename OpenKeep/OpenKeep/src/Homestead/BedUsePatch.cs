using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Bed.Interact</c> prefix and postfix on the using player's client. With the setting on, using an owned bed
    /// that is not the spawn point first makes it the profile's spawn point (quietly), so the game's own method takes
    /// its sleep path with its usual checks (night, enemies, roof and cover, fire, wet) instead of only moving the
    /// spawn point. The postfix remembers the bed when the game (or the prefix) made it the spawn point: a claim that
    /// passed the game's roof check, a use of an owned bed. Off: the game's behaviour, the bed still remembered.
    /// </summary>
    [HarmonyPatch(typeof(Bed), nameof(Bed.Interact))]
    public static class BedUsePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Bed __instance, bool repeat, ref bool __state)
        {
            __state = false;
            if (repeat || !BedChecks.IsLive(__instance) || Game.instance == null)
                return;
            bool mine = BedChecks.IsLocal(__instance);
            __state = mine || BedChecks.IsUnclaimed(__instance);
            if (mine && BedSettings.NearestBedRespawn.Value && !BedChecks.IsSpawnPoint(__instance))
                Game.instance.GetPlayerProfile().SetCustomSpawnPoint(__instance.GetSpawnPoint());
        }

        [HarmonyPostfix]
        public static void Postfix(Bed __instance, bool __state)
        {
            if (__state && BedChecks.IsSpawnPoint(__instance))
                BedList.Remember(__instance.GetSpawnPoint());
        }
    }
}
