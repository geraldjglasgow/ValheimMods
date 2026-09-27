using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Fermenting speed from the Cooking level of whoever put the base in. The barrel stores only a start time;
    /// GetFermentationTime returns the world seconds since then (-1 when empty), and GetStatus compares that with
    /// m_fermentationDuration. Every readiness decision (status, visuals, hover, interact, the owner's tap check,
    /// DropAllItems) goes through GetStatus, so scaling the elapsed time here speeds up all of them on every peer
    /// alike, from the level in the barrel's ZDO. m_fermentationDuration is left alone: FeastMaster replaces it for
    /// each GetStatus call, and the two compose.
    /// </summary>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetFermentationTime))]
    internal static class FermenterSpeed
    {
        [HarmonyPostfix]
        private static void Postfix(Fermenter __instance, ref double __result)
        {
            if (__result <= 0.0)
                return;
            float level = FermenterBase.GetLevel(__instance.m_nview.GetZDO());
            if (level > 0f)
                __result *= Perks.FermentingSpeed(level);
        }
    }
}
