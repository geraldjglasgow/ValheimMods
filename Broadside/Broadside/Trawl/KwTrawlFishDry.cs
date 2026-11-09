using HarmonyLib;

namespace Broadside
{
    /// <summary>
    /// A dead fish out of the trawl (<see cref="KwTrawlDead"/>) is out of the water: the game lets a fish be picked up only
    /// out of it, and the sea round a ship reaches over a deck near the waterline.
    /// </summary>
    [HarmonyPatch(typeof(Fish), nameof(Fish.IsOutOfWater))]
    public static class KwTrawlFishDry
    {
        [HarmonyPostfix]
        private static void Postfix(Fish __instance, ref bool __result)
        {
            if (!__result && KwTrawlDead.Is(__instance))
                __result = true;
        }
    }
}
