using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Bed.GetHoverText</c> postfix: with the setting on, every owned bed is a spawn bed, so an owned bed that is
    /// not the current one offers the game's own "Sleep" instead of "Set spawn point" (<see cref="BedUsePatch"/>
    /// makes the use sleep). The words are swapped in the game's text, so anything another mod added stays.
    /// </summary>
    [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverText))]
    public static class BedHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Bed __instance, ref string __result)
        {
            if (!BedSettings.NearestBedRespawn.Value || string.IsNullOrEmpty(__result) || !BedChecks.IsLive(__instance))
                return;
            if (!BedChecks.IsLocal(__instance) || __instance.IsCurrent())
                return;
            __result = __result.Replace(Language.Localize("$piece_bed_setspawn"), Language.Localize("$piece_bed_sleep"));
        }
    }
}
